using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Certiva.Application.Attempts;
using Certiva.Infrastructure.Data;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Infrastructure.Data.Queries;

public sealed class EfExamAttemptAnswerStore : IExamAttemptAnswerStore
{
    private readonly ApplicationDbContext _db;
    public EfExamAttemptAnswerStore(ApplicationDbContext db) => _db = db;

    public Task<SaveAnswerResultDto> SaveAnswerAsync(SaveAnswerRequest request, string? userId, string? accessToken, CancellationToken cancellationToken = default) => SaveCoreAsync(request, userId, accessToken, 1, cancellationToken);

    private async Task<SaveAnswerResultDto> SaveCoreAsync(SaveAnswerRequest request, string? userId, string? accessToken, int retries, CancellationToken ct)
    {
        var attempt = await _db.Set<ExamAttempt>().FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct);
        if (attempt == null || (attempt.UserId != null && !string.Equals(attempt.UserId, userId, StringComparison.Ordinal)) || !TokenMatches(accessToken ?? string.Empty, attempt.AnonymousTokenHash)) return new(SaveAnswerResultStatus.NotFoundOrUnauthorized);
        if (attempt.Status != ExamAttemptStatus.InProgress) return new(SaveAnswerResultStatus.AttemptClosed);
        var now = DateTime.UtcNow;
        if (attempt.ExpiresAtUtc.HasValue && attempt.ExpiresAtUtc.Value <= now)
        {
            attempt.Status = ExamAttemptStatus.Expired; attempt.CompletedAtUtc = now; attempt.LastModifiedOnUtc = now;
            await _db.SaveChangesAsync(ct); return new(SaveAnswerResultStatus.AttemptClosed);
        }
        var question = await _db.Set<ExamAttemptQuestion>().Include(q => q.Choices).FirstOrDefaultAsync(q => q.Id == request.AttemptQuestionId && q.ExamAttemptId == request.AttemptId, ct);
        if (question == null) return new(SaveAnswerResultStatus.NotFoundOrUnauthorized);
        var selected = request.SelectedChoiceIds.Distinct().ToList();
        if (selected.Count != request.SelectedChoiceIds.Count || selected.Any(id => question.Choices.All(c => c.Id != id))) return new(SaveAnswerResultStatus.InvalidAnswer);
        var text = request.TextAnswer?.Trim();
        if (text?.Length > 4000 || (question.QuestionTypeSnapshot == QuestionType.ShortAnswer && selected.Count > 0) || (question.QuestionTypeSnapshot != QuestionType.ShortAnswer && !string.IsNullOrWhiteSpace(text)) || (question.QuestionTypeSnapshot is QuestionType.SingleChoice or QuestionType.TrueFalse && selected.Count > 1)) return new(SaveAnswerResultStatus.InvalidAnswer);
        var answer = await _db.Set<ExamAnswer>().FirstOrDefaultAsync(a => a.ExamAttemptQuestionId == question.Id, ct);
        var isNew = answer == null;
        answer ??= new ExamAnswer { Id = Guid.NewGuid(), ExamAttemptQuestionId = question.Id, CreatedOnUtc = now };
        answer.SelectedChoiceIdsJson = selected.Count == 0 ? null : JsonSerializer.Serialize(selected); answer.TextAnswer = string.IsNullOrWhiteSpace(text) ? null : text; answer.SavedAtUtc = now; answer.IsCorrect = null; answer.ScoreAwarded = 0m; answer.LastModifiedOnUtc = now; attempt.LastModifiedOnUtc = now;
        if (isNew) _db.Set<ExamAnswer>().Add(answer);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) when (retries > 0)
        {
            _db.ChangeTracker.Clear(); var current = await _db.Set<ExamAttempt>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct);
            if (current == null || current.Status != ExamAttemptStatus.InProgress || (current.ExpiresAtUtc.HasValue && current.ExpiresAtUtc.Value <= DateTime.UtcNow)) return new(SaveAnswerResultStatus.AttemptClosed);
            return await SaveCoreAsync(request, userId, accessToken, retries - 1, ct);
        }
        catch (DbUpdateException) when (isNew)
        {
            _db.Entry(answer).State = EntityState.Detached; var concurrent = await _db.Set<ExamAnswer>().FirstOrDefaultAsync(a => a.ExamAttemptQuestionId == question.Id, ct); if (concurrent == null) throw;
            concurrent.SelectedChoiceIdsJson = answer.SelectedChoiceIdsJson; concurrent.TextAnswer = answer.TextAnswer; concurrent.SavedAtUtc = now; concurrent.IsCorrect = null; concurrent.ScoreAwarded = 0m; concurrent.LastModifiedOnUtc = now; await _db.SaveChangesAsync(ct);
        }
        return new(SaveAnswerResultStatus.Saved, now);
    }
    private static bool TokenMatches(string token, string? stored) { if (string.IsNullOrWhiteSpace(stored)) return false; try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(token)), Convert.FromHexString(stored)); } catch (FormatException) { return false; } }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
