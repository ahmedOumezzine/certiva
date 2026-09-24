using System.Net.Mail;
using System.Security.Cryptography;
using Certiva.Application.Attempts;
using Certiva.Application.Exams;
using Certiva.Application.Security;
using Certiva.Infrastructure.Data;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Infrastructure.Data.Queries;

public sealed class EfExamAttemptStartStore : IExamAttemptStartStore
{
    private readonly ApplicationDbContext _db;
    private readonly IGuestAttemptTokenService _tokens;
    public EfExamAttemptStartStore(ApplicationDbContext db, IGuestAttemptTokenService tokens) { _db = db; _tokens = tokens; }

    public async Task<StartedAttemptDto?> StartAsync(StartAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var culture = request.Culture == "en" ? "en" : "fr";
        var email = request.GuestEmail?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Slug) || email.Length is 0 or > 256 || !MailAddress.TryCreate(email, out var parsed) || !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)) return null;
        email = email.ToLowerInvariant();
        var exam = await _db.Set<Exam>().AsNoTracking().Include(e => e.Translations).Include(e => e.Skills!).ThenInclude(s => s.Translations).Include(e => e.Questions!).ThenInclude(q => q.Translations).Include(e => e.Questions!).ThenInclude(q => q.Choices!).ThenInclude(c => c.Translations).Include(e => e.Questions!).ThenInclude(q => q.Skill!).ThenInclude(s => s!.Translations).FirstOrDefaultAsync(e => e.Slug == request.Slug && e.Status == Status.Published, cancellationToken);
        if (exam == null || !ExamPublicationRules.IsPublishable(exam)) return null;
        if (request.SkillId.HasValue && request.SkillId.Value != Guid.Empty && !(exam.Skills?.Any(s => s.Id == request.SkillId.Value && s.ExamId == exam.Id) ?? false)) return null;
        var source = exam.Questions!.Where(q => q.Status == Status.Published && (!request.SkillId.HasValue || request.SkillId.Value == Guid.Empty || q.SkillId == request.SkillId.Value)).ToList();
        if (source.Count == 0) return null;
        var token = _tokens.GenerateToken(); var now = DateTime.UtcNow; var et = exam.Translations.FirstOrDefault(t => t.Culture == culture); var questions = request.Random ? source.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList() : source;
        var attempt = new ExamAttempt { Id = Guid.NewGuid(), ExamId = exam.Id, ExamNameSnapshot = Localized(et?.Name, exam.Name), ExamDescriptionSnapshot = Localized(et?.Description, exam.Description), ExamSlugSnapshot = Localized(et?.Slug, exam.Slug), Culture = culture, PassingPercentageSnapshot = exam.PassingPercentage, UserId = null, GuestEmail = email, AnonymousTokenHash = _tokens.HashToken(token), StartedAtUtc = now, ExpiresAtUtc = exam.DurationMinutes.HasValue ? now.AddMinutes(exam.DurationMinutes.Value) : null, Status = ExamAttemptStatus.InProgress, CreatedOnUtc = now,
            Questions = questions.Select((q, index) => { var choices = q.Choices?.ToList() ?? []; if (request.Random) choices = choices.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList(); return new ExamAttemptQuestion { Id = Guid.NewGuid(), SourceQuestionId = q.Id, Order = index + 1, Points = 1m, QuestionTextSnapshot = Localized(q.Translations.FirstOrDefault(t => t.Culture == culture)?.Description, q.Description), ExplanationSnapshot = Localized(q.Translations.FirstOrDefault(t => t.Culture == culture)?.Explanation, q.Explication), QuestionTypeSnapshot = q.QuestionType, SkillNameSnapshot = Localized(q.Skill?.Translations.FirstOrDefault(t => t.Culture == culture)?.Name, q.Skill?.Name), CreatedOnUtc = now, Choices = choices.Select((c, choiceIndex) => new ExamAttemptChoice { Id = Guid.NewGuid(), SourceChoiceId = c.Id, Order = choiceIndex + 1, ChoiceTextSnapshot = Localized(c.Translations.FirstOrDefault(t => t.Culture == culture)?.ChoiceText, c.ChoiceText), GroupBySnapshot = Localized(c.Translations.FirstOrDefault(t => t.Culture == culture)?.GroupBy, c.GroupBy), IsCorrectSnapshot = c.IsCorrect, CreatedOnUtc = now }).ToList() }; }).ToList() };
        _db.Set<ExamAttempt>().Add(attempt); await _db.SaveChangesAsync(cancellationToken); return new StartedAttemptDto(attempt.Id, token);
    }
    private static string Localized(string? translated, string? fallback) => string.IsNullOrWhiteSpace(translated) ? fallback ?? string.Empty : translated;
}

public sealed class EfExamAttemptSubmitStore : IExamAttemptSubmitStore
{
    private readonly ApplicationDbContext _db;
    private readonly IGuestAttemptTokenService _tokens;
    public EfExamAttemptSubmitStore(ApplicationDbContext db, IGuestAttemptTokenService tokens) { _db = db; _tokens = tokens; }

    public async Task<AttemptResultDto?> SubmitAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default)
    {
        for (var retry = 0; retry < 2; retry++)
        {
            try { return await SubmitOnceAsync(attemptId, userId, accessToken, cancellationToken); }
            catch (DbUpdateConcurrencyException) when (retry == 0)
            {
                _db.ChangeTracker.Clear(); var latest = await LoadAsync(attemptId, cancellationToken);
                if (!CanAccess(latest, userId, accessToken)) return null;
                if (latest!.Score.HasValue && latest.Status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired) return BuildResult(latest);
            }
        }
        return null;
    }

    private async Task<AttemptResultDto?> SubmitOnceAsync(Guid id, string? userId, string? token, CancellationToken ct)
    {
        var attempt = await LoadAsync(id, ct); if (!CanAccess(attempt, userId, token)) return null;
        if (attempt!.Score.HasValue && attempt.Status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired) return BuildResult(attempt);
        if (attempt.Status == ExamAttemptStatus.Completed) return null;
        var now = DateTime.UtcNow; var expired = attempt.Status == ExamAttemptStatus.Expired || (attempt.ExpiresAtUtc.HasValue && attempt.ExpiresAtUtc.Value <= now); decimal max = 0; decimal score = 0;
        foreach (var question in attempt.Questions.OrderBy(q => q.Order))
        {
            max += question.Points; var answer = question.Answer; var selected = Deserialize(answer?.SelectedChoiceIdsJson); var correct = question.Choices.Where(c => c.IsCorrectSnapshot).ToList(); var isCorrect = answer != null && IsAnswerCorrect(question.QuestionTypeSnapshot, selected, answer.TextAnswer, correct); var awarded = isCorrect ? question.Points : 0m; score += awarded;
            if (answer != null) { answer.IsCorrect = isCorrect; answer.ScoreAwarded = awarded; answer.LastModifiedOnUtc = now; }
        }
        var percentage = max <= 0 ? 0 : (int)Math.Round(score * 100m / max, MidpointRounding.AwayFromZero); attempt.Score = score; attempt.MaxScore = max; attempt.Percentage = percentage; attempt.Passed = percentage >= attempt.PassingPercentageSnapshot; attempt.Status = expired ? ExamAttemptStatus.Expired : ExamAttemptStatus.Completed; attempt.CompletedAtUtc = now; attempt.LastModifiedOnUtc = now;
        await _db.SaveChangesAsync(ct); return BuildResult(attempt);
    }

    private Task<ExamAttempt?> LoadAsync(Guid id, CancellationToken ct) => _db.Set<ExamAttempt>().Include(a => a.Questions!).ThenInclude(q => q.Choices).Include(a => a.Questions!).ThenInclude(q => q.Answer).FirstOrDefaultAsync(a => a.Id == id, ct);
    private bool CanAccess(ExamAttempt? a, string? userId, string? token) => a != null && (a.UserId == null || string.Equals(a.UserId, userId, StringComparison.Ordinal)) && _tokens.Matches(token ?? string.Empty, a.AnonymousTokenHash);
    private static bool IsAnswerCorrect(QuestionType type, IReadOnlyCollection<Guid> selected, string? text, IReadOnlyCollection<ExamAttemptChoice> correct) => type == QuestionType.ShortAnswer ? Normalize(text).Length > 0 && correct.Any(c => Normalize(c.ChoiceTextSnapshot) == Normalize(text)) : selected.Count > 0 && correct.Select(c => c.Id).ToHashSet().SetEquals(selected);
    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static List<Guid> Deserialize(string? json) { if (string.IsNullOrWhiteSpace(json)) return []; try { return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? []; } catch (System.Text.Json.JsonException) { return []; } }
    private static AttemptResultDto BuildResult(ExamAttempt attempt)
    {
        var questions = attempt.Questions.OrderBy(q => q.Order).Select(q => { var answer = q.Answer; var selected = Deserialize(answer?.SelectedChoiceIdsJson).ToHashSet(); var correct = q.Choices.Where(c => c.IsCorrectSnapshot).ToList(); return new AttemptQuestionResultDto(q.SourceQuestionId, q.Id, q.QuestionTextSnapshot, q.SkillNameSnapshot ?? "General", q.QuestionTypeSnapshot, answer?.IsCorrect == true, q.Points, answer?.ScoreAwarded ?? 0m, q.ExplanationSnapshot, selected.Count > 0 ? q.Choices.Where(c => selected.Contains(c.Id)).OrderBy(c => c.Order).Select(c => c.ChoiceTextSnapshot).ToList() : string.IsNullOrWhiteSpace(answer?.TextAnswer) ? [] : [answer.TextAnswer], correct.OrderBy(c => c.Order).Select(c => c.ChoiceTextSnapshot).ToList()); }).ToList();
        return new AttemptResultDto(NormalizeCulture(attempt.Culture), attempt.ExamNameSnapshot, attempt.ExamSlugSnapshot, questions.Count, questions.Count(q => q.IsCorrect), attempt.Score ?? 0m, attempt.MaxScore ?? 0m, (int)Math.Round(attempt.Percentage ?? 0m), attempt.Passed == true, attempt.PassingPercentageSnapshot, Math.Max(0, (int)((attempt.CompletedAtUtc ?? DateTime.UtcNow) - attempt.StartedAtUtc).TotalSeconds), questions.GroupBy(q => q.SkillName).Select(g => new SkillResultDto(g.Key, g.Count(), g.Count(q => q.IsCorrect))).OrderBy(s => s.SkillName).ToList(), questions, attempt.Status);
    }
    private static string NormalizeCulture(string? culture) => culture == "en" ? "en" : "fr";
}
