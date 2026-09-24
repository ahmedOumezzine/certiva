using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Certiva.Application.Attempts;
using Certiva.Infrastructure.Data;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Infrastructure.Data.Queries;

public sealed class EfExamAttemptReader : IExamAttemptReader
{
    private readonly ApplicationDbContext _db;
    public EfExamAttemptReader(ApplicationDbContext db) => _db = db;

    public async Task<ResumeAttemptResultDto> ResumeAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default)
    {
        if (attemptId == Guid.Empty || string.IsNullOrWhiteSpace(accessToken)) return new(ResumeAttemptResultStatus.NotFoundOrUnauthorized);
        var attempt = await LoadAsync(attemptId, cancellationToken);
        if (!CanAccess(attempt, userId, accessToken)) return new(ResumeAttemptResultStatus.NotFoundOrUnauthorized);
        if (attempt!.Status != ExamAttemptStatus.InProgress) return new(ResumeAttemptResultStatus.Expired);
        var now = DateTime.UtcNow;
        if (attempt.ExpiresAtUtc.HasValue && attempt.ExpiresAtUtc.Value <= now)
        {
            await _db.Set<ExamAttempt>().Where(a => a.Id == attemptId && a.Status == ExamAttemptStatus.InProgress).ExecuteUpdateAsync(u => u.SetProperty(a => a.Status, ExamAttemptStatus.Expired).SetProperty(a => a.CompletedAtUtc, now).SetProperty(a => a.LastModifiedOnUtc, now), cancellationToken);
            return new(ResumeAttemptResultStatus.Expired);
        }
        var questions = attempt.Questions.OrderBy(q => q.Order).Select(q => new AttemptQuestionDto(q.SourceQuestionId, q.Id, q.Order, q.QuestionTextSnapshot, q.QuestionTypeSnapshot, q.SkillNameSnapshot ?? "General", Deserialize(q.Answer?.SelectedChoiceIdsJson).Select(id => q.Choices.FirstOrDefault(c => c.Id == id)?.SourceChoiceId ?? Guid.Empty).Where(id => id != Guid.Empty).ToList(), q.Answer?.TextAnswer, q.Choices.OrderBy(c => c.Order).Select(c => new AttemptChoiceDto(c.SourceChoiceId, c.Id, c.Order, c.ChoiceTextSnapshot, c.GroupBySnapshot)).ToList())).ToList();
        return new(ResumeAttemptResultStatus.Ready, new(attempt.Id, attempt.ExamId, NormalizeCulture(attempt.Culture), attempt.ExamNameSnapshot, string.IsNullOrWhiteSpace(attempt.ExamDescriptionSnapshot) ? attempt.Exam?.Description ?? string.Empty : attempt.ExamDescriptionSnapshot, attempt.Exam?.Code ?? string.Empty, attempt.ExamSlugSnapshot, attempt.PassingPercentageSnapshot, attempt.Exam?.DurationMinutes, attempt.StartedAtUtc, attempt.ExpiresAtUtc, attempt.ExpiresAtUtc.HasValue ? Math.Max(0, (int)Math.Ceiling((attempt.ExpiresAtUtc.Value - now).TotalSeconds)) : 0, questions));
    }

    public async Task<AttemptResultDto?> GetResultAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAsync(attemptId, cancellationToken);
        if (!CanAccess(attempt, userId, accessToken) || !attempt!.Score.HasValue || attempt.Status is not (ExamAttemptStatus.Completed or ExamAttemptStatus.Expired)) return null;
        var questions = attempt.Questions.OrderBy(q => q.Order).Select(q =>
        {
            var answer = q.Answer; var selected = Deserialize(answer?.SelectedChoiceIdsJson).ToHashSet();
            return new AttemptQuestionResultDto(q.SourceQuestionId, q.Id, q.QuestionTextSnapshot, q.SkillNameSnapshot ?? "General", q.QuestionTypeSnapshot, answer?.IsCorrect == true, q.Points, answer?.ScoreAwarded ?? 0m, q.ExplanationSnapshot, selected.Count > 0 ? q.Choices.Where(c => selected.Contains(c.Id)).OrderBy(c => c.Order).Select(c => c.ChoiceTextSnapshot).ToList() : string.IsNullOrWhiteSpace(answer?.TextAnswer) ? [] : [answer.TextAnswer], q.Choices.Where(c => c.IsCorrectSnapshot).OrderBy(c => c.Order).Select(c => c.ChoiceTextSnapshot).ToList());
        }).ToList();
        return new(NormalizeCulture(attempt.Culture), attempt.ExamNameSnapshot, attempt.ExamSlugSnapshot, questions.Count, questions.Count(q => q.IsCorrect), attempt.Score.Value, attempt.MaxScore ?? 0m, (int)Math.Round(attempt.Percentage ?? 0m), attempt.Passed == true, attempt.PassingPercentageSnapshot, Math.Max(0, (int)((attempt.CompletedAtUtc ?? DateTime.UtcNow) - attempt.StartedAtUtc).TotalSeconds), questions.GroupBy(q => q.SkillName).Select(g => new SkillResultDto(g.Key, g.Count(), g.Count(q => q.IsCorrect))).OrderBy(s => s.SkillName).ToList(), questions, attempt.Status);
    }

    private Task<ExamAttempt?> LoadAsync(Guid id, CancellationToken ct) => _db.Set<ExamAttempt>().AsNoTracking().Include(a => a.Exam).Include(a => a.Questions!).ThenInclude(q => q.Choices).Include(a => a.Questions!).ThenInclude(q => q.Answer).FirstOrDefaultAsync(a => a.Id == id, ct);
    private static bool CanAccess(ExamAttempt? a, string? userId, string? token) => a != null && (a.UserId == null || string.Equals(a.UserId, userId, StringComparison.Ordinal)) && TokenMatches(token ?? string.Empty, a.AnonymousTokenHash);
    private static string NormalizeCulture(string? culture) => culture == "en" ? "en" : "fr";
    private static List<Guid> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Guid>>(json) ?? []; } catch (JsonException) { return []; }
    }
    private static bool TokenMatches(string token, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(HashToken(token)), Convert.FromHexString(storedHash)); } catch (FormatException) { return false; }
    }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
