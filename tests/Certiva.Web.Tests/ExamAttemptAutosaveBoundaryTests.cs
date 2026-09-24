using Certiva.Application.Attempts;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Certiva.Services.Exams;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamAttemptAutosaveBoundaryTests
{
    [Fact]
    public async Task AutosavePersistsAndUpdatesAnAnswerThroughApplicationBoundary()
    {
        await using var db = NewDb();
        var exam = new Exam { Id = Guid.NewGuid(), Name = "Exam", Description = "Description", Code = "EX", Slug = "exam", Status = Status.Published, PassingPercentage = 70, CreatedOnUtc = DateTime.UtcNow, Questions = [new Question { Id = Guid.NewGuid(), Description = "Question", QuestionType = QuestionType.ShortAnswer, Status = Status.Published, CreatedOnUtc = DateTime.UtcNow, Choices = [new Choice { Id = Guid.NewGuid(), ChoiceText = "answer", IsCorrect = true, CreatedOnUtc = DateTime.UtcNow }] }] };
        db.Exams.Add(exam); await db.SaveChangesAsync();
        var started = await StartTestHelpers.StartAsync(db, exam.Slug!);
        var question = await db.ExamAttemptQuestions.SingleAsync();
        var service = new ExamAttemptAutosaveService(new EfExamAttemptAnswerStore(db));

        var first = await service.SaveAnswerAsync(new(started!.AttemptId, question.Id, [], " first answer "), null, started.AccessToken);
        var savedAt = first.SavedAtUtc;
        var second = await service.SaveAnswerAsync(new(started.AttemptId, question.Id, [], "updated answer"), null, started.AccessToken);
        var answer = await db.ExamAnswers.SingleAsync();

        Assert.Equal(SaveAnswerResultStatus.Saved, first.Status);
        Assert.Equal(SaveAnswerResultStatus.Saved, second.Status);
        Assert.Equal("updated answer", answer.TextAnswer);
        Assert.Null(answer.IsCorrect);
        Assert.Equal(0m, answer.ScoreAwarded);
        Assert.True(second.SavedAtUtc >= savedAt);
    }

    [Fact]
    public async Task AutosaveRejectsMissingInvalidTokenAndInvalidQuestion()
    {
        await using var db = NewDb();
        var exam = new Exam { Id = Guid.NewGuid(), Name = "Exam", Description = "Description", Code = "EX", Slug = "exam", Status = Status.Published, CreatedOnUtc = DateTime.UtcNow, Questions = [new Question { Id = Guid.NewGuid(), Description = "Question", QuestionType = QuestionType.ShortAnswer, Status = Status.Published, CreatedOnUtc = DateTime.UtcNow, Choices = [new Choice { Id = Guid.NewGuid(), ChoiceText = "answer", IsCorrect = true, CreatedOnUtc = DateTime.UtcNow }] }] };
        db.Exams.Add(exam); await db.SaveChangesAsync();
        var started = await StartTestHelpers.StartAsync(db, exam.Slug!);
        var question = await db.ExamAttemptQuestions.SingleAsync();
        var service = new ExamAttemptAutosaveService(new EfExamAttemptAnswerStore(db));

        var missing = await service.SaveAnswerAsync(new(started!.AttemptId, question.Id, [], "answer"), null, null);
        var invalid = await service.SaveAnswerAsync(new(started.AttemptId, Guid.NewGuid(), [], "answer"), null, started.AccessToken);

        Assert.Equal(SaveAnswerResultStatus.NotFoundOrUnauthorized, missing.Status);
        Assert.Equal(SaveAnswerResultStatus.NotFoundOrUnauthorized, invalid.Status);
    }

    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
