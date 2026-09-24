using Certiva.Application.Attempts;
using Certiva.Application.Security;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamAttemptStartBoundaryTests
{
    [Fact]
    public async Task ApplicationStartPersistsLocalizedSnapshotsAndNeverPersistsRawToken()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var examId = Guid.NewGuid(); var questionId = Guid.NewGuid(); var choiceId = Guid.NewGuid(); var now = DateTime.UtcNow;
        db.Exams.Add(new Exam
        {
            Id = examId, Name = "Examen FR", Description = "Description FR", Code = "EX", Slug = "examen-fr", Status = Status.Published, PassingPercentage = 75, DurationMinutes = 20, CreatedOnUtc = now,
            Questions = [new Question { Id = questionId, ExamId = examId, Description = "Question FR", Explication = "Explication FR", QuestionType = QuestionType.SingleChoice, Status = Status.Published, CreatedOnUtc = now, Choices = [new Choice { Id = choiceId, ChoiceText = "Choix FR", GroupBy = "Groupe", IsCorrect = true, CreatedOnUtc = now }, new Choice { Id = Guid.NewGuid(), ChoiceText = "Autre choix", CreatedOnUtc = now }], Translations = [new QuestionTranslation { Id = Guid.NewGuid(), QuestionId = questionId, Culture = "en", Description = "Question EN", Explanation = "Explanation EN", CreatedOnUtc = now }] }],
            Translations = [new ExamTranslation { Id = Guid.NewGuid(), ExamId = examId, Culture = "en", Name = "Exam EN", Description = "Description EN", Slug = "exam-en", CreatedOnUtc = now }]
        });
        await db.SaveChangesAsync();
        var start = new ExamAttemptStartService(new EfExamAttemptStartStore(db, new GuestAttemptTokenService()));

        var result = await start.StartAsync(new StartAttemptRequest("examen-fr", null, false, " Guest@Example.com ", "en"));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
        var attempt = await db.ExamAttempts.Include(a => a.Questions!).ThenInclude(q => q.Choices).SingleAsync();
        Assert.Equal("Exam EN", attempt.ExamNameSnapshot);
        Assert.Equal("Description EN", attempt.ExamDescriptionSnapshot);
        Assert.Equal(75, attempt.PassingPercentageSnapshot);
        Assert.Equal(20, attempt.ExpiresAtUtc!.Value.Subtract(attempt.StartedAtUtc).Minutes);
        Assert.Equal("Question EN", Assert.Single(attempt.Questions).QuestionTextSnapshot);
        Assert.Contains(attempt.Questions.Single().Choices, choice => choice.ChoiceTextSnapshot == "Choix FR");
        Assert.NotEqual(result.AccessToken, attempt.AnonymousTokenHash);
    }
}
