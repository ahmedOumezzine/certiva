using System.Security.Cryptography;
using System.Text;
using Certiva.Application.Attempts;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamAttemptReadSecurityTests
{
    [Fact]
    public async Task ResumeRequiresValidGuestTokenAndPreservesSnapshot()
    {
        await using var db = NewDb();
        var (attempt, token) = await SeedAsync(db, ExamAttemptStatus.InProgress, userId: "owner");
        var reader = new EfExamAttemptReader(db);
        Assert.Equal(Hash(token), (await db.ExamAttempts.SingleAsync()).AnonymousTokenHash);

        var valid = await reader.ResumeAsync(attempt.Id, "owner", token);
        var missing = await reader.ResumeAsync(attempt.Id, null, null);
        var malformed = await reader.ResumeAsync(attempt.Id, null, "not-a-valid-token");
        var wrongUser = await reader.ResumeAsync(attempt.Id, "user-1", token);
        var missingAttempt = await reader.ResumeAsync(Guid.NewGuid(), null, token);

        Assert.Equal(ResumeAttemptResultStatus.Ready, valid.Status);
        Assert.Equal("en", valid.Session!.Culture);
        Assert.Equal("Frozen exam", valid.Session.ExamName);
        Assert.Equal("Frozen question", Assert.Single(valid.Session.Questions).Description);
        Assert.Equal(ResumeAttemptResultStatus.NotFoundOrUnauthorized, missing.Status);
        Assert.Equal(ResumeAttemptResultStatus.NotFoundOrUnauthorized, malformed.Status);
        Assert.Equal(ResumeAttemptResultStatus.NotFoundOrUnauthorized, wrongUser.Status);
        Assert.Equal(ResumeAttemptResultStatus.NotFoundOrUnauthorized, missingAttempt.Status);
        Assert.DoesNotContain(token, valid.Session.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("AnonymousTokenHash", valid.Session.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResumeMarksExpiredAndRejectsCompletedAttempts()
    {
        await using var db = NewDb();
        var (expired, token) = await SeedAsync(db, ExamAttemptStatus.Expired, DateTime.UtcNow.AddMinutes(-1));
        var reader = new EfExamAttemptReader(db);

        var expiredResult = await reader.ResumeAsync(expired.Id, null, token);
        Assert.Equal(ResumeAttemptResultStatus.Expired, expiredResult.Status);
        Assert.Equal(ExamAttemptStatus.Expired, (await db.ExamAttempts.SingleAsync()).Status);

        var completed = await SeedAsync(db, ExamAttemptStatus.Completed);
        var completedResult = await reader.ResumeAsync(completed.Attempt.Id, null, completed.Token);
        Assert.Equal(ResumeAttemptResultStatus.Expired, completedResult.Status);
    }

    [Fact]
    public async Task ResultRequiresAccessAndIsUnavailableBeforeSubmission()
    {
        await using var db = NewDb();
        var (attempt, token) = await SeedAsync(db, ExamAttemptStatus.InProgress, userId: "owner");
        var reader = new EfExamAttemptReader(db);

        Assert.Null(await reader.GetResultAsync(attempt.Id, null, token));
        Assert.Null(await reader.GetResultAsync(attempt.Id, null, null));
        Assert.Null(await reader.GetResultAsync(attempt.Id, null, "bad-token"));
        Assert.Null(await reader.GetResultAsync(attempt.Id, "wrong-user", token));
        Assert.Null(await reader.GetResultAsync(Guid.NewGuid(), null, token));
    }

    [Theory]
    [InlineData(ExamAttemptStatus.Completed)]
    [InlineData(ExamAttemptStatus.Expired)]
    public async Task ResultUsesPersistedScoreAndSnapshots(ExamAttemptStatus status)
    {
        await using var db = NewDb();
        var (attempt, token) = await SeedAsync(db, status);
        var reader = new EfExamAttemptReader(db);

        var result = await reader.GetResultAsync(attempt.Id, null, token);

        Assert.NotNull(result);
        Assert.Equal(12.5m, result!.Score);
        Assert.Equal(20m, result.MaxScore);
        Assert.Equal(63, result.Percentage);
        Assert.False(result.Passed);
        Assert.Equal("Frozen exam", result.ExamName);
        Assert.Equal("Frozen question", Assert.Single(result.Questions).Description);
        Assert.DoesNotContain(token, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("AnonymousTokenHash", result.ToString(), StringComparison.Ordinal);
    }

    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(ExamAttempt Attempt, string Token)> SeedAsync(ApplicationDbContext db, ExamAttemptStatus status, DateTime? expiresAtUtc = null, string? userId = null)
    {
        var token = "read-test-token";
        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(), ExamId = Guid.NewGuid(), ExamNameSnapshot = "Frozen exam", ExamDescriptionSnapshot = "Frozen description", ExamSlugSnapshot = "frozen-exam", Culture = "en", UserId = userId, PassingPercentageSnapshot = 70, AnonymousTokenHash = Hash(token), StartedAtUtc = DateTime.UtcNow.AddMinutes(-5), ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(20), Status = status, Score = status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired ? 12.5m : null, MaxScore = status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired ? 20m : null, Percentage = status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired ? 63 : null, Passed = status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired ? false : null, CompletedAtUtc = status is ExamAttemptStatus.Completed or ExamAttemptStatus.Expired ? DateTime.UtcNow : null, CreatedOnUtc = DateTime.UtcNow, Exam = new Exam { Id = Guid.NewGuid(), Name = "Current exam", Description = "Current description", Code = "CUR", Slug = "current-exam", Status = Status.Published, CreatedOnUtc = DateTime.UtcNow },
            Questions = [new ExamAttemptQuestion { Id = Guid.NewGuid(), SourceQuestionId = Guid.NewGuid(), Order = 1, Points = 20m, QuestionTextSnapshot = "Frozen question", QuestionTypeSnapshot = QuestionType.SingleChoice, SkillNameSnapshot = "Frozen skill", CreatedOnUtc = DateTime.UtcNow, Choices = [new ExamAttemptChoice { Id = Guid.NewGuid(), SourceChoiceId = Guid.NewGuid(), Order = 1, ChoiceTextSnapshot = "Frozen answer", IsCorrectSnapshot = true, CreatedOnUtc = DateTime.UtcNow }] }]
        };
        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync();
        return (await db.ExamAttempts.AsNoTracking().OrderByDescending(a => a.CreatedOnUtc).FirstAsync(), token);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
