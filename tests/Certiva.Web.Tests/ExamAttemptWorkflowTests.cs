using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Infrastructure.Data;
using Certiva.Services.Exams;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamAttemptWorkflowTests
{
    [Fact]
    public async Task AttemptKeepsItsStartingCultureAndExamDescriptionSnapshot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var examId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using var db = new ApplicationDbContext(options);
        var exam = new Exam
        {
            Id = examId,
            Name = "Examen français",
            Description = "Description française",
            Code = "EX",
            Slug = "examen-francais",
            Status = Status.Published,
            PassingPercentage = 70,
            CreatedOnUtc = now,
            Skills = [],
            Translations =
            [
                new ExamTranslation
                {
                    Id = Guid.NewGuid(),
                    ExamId = examId,
                    Culture = "en",
                    Name = "English exam",
                    Description = "English description",
                    Slug = "english-exam",
                    CreatedOnUtc = now
                }
            ],
            Questions =
            [
                new Question
                {
                    Id = questionId,
                    ExamId = examId,
                    Description = "Question française",
                    QuestionType = QuestionType.ShortAnswer,
                    Status = Status.Published,
                    CreatedOnUtc = now,
                    Translations =
                    [
                        new QuestionTranslation
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = questionId,
                            Culture = "en",
                            Description = "English question",
                            CreatedOnUtc = now
                        }
                    ],
                    Choices =
                    [
                        new Choice
                        {
                            Id = Guid.NewGuid(),
                            ChoiceText = "Réponse",
                            IsCorrect = true,
                            CreatedOnUtc = now,
                            Translations =
                            [
                                new ChoiceTranslation
                                {
                                    Id = Guid.NewGuid(),
                                    Culture = "en",
                                    ChoiceText = "Answer",
                                    CreatedOnUtc = now
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();

        var started = await StartTestHelpers.StartAsync(db, exam.Slug!, email: "candidate@example.com", culture: "en");

        Assert.NotNull(started);
        var attempt = await db.Set<ExamAttempt>().SingleAsync();
        Assert.Equal("en", attempt.Culture);
        Assert.Equal("English exam", attempt.ExamNameSnapshot);
        Assert.Equal("English description", attempt.ExamDescriptionSnapshot);

        exam.Translations.Single().Name = "Changed English title";
        exam.Translations.Single().Description = "Changed English description";
        await db.SaveChangesAsync();

        var resumed = await ReadTestHelpers.ResumeAsync(db, started!.AttemptId, null, started.AccessToken);

        Assert.Equal(ResumeAttemptStatus.Ready, resumed.Status);
        Assert.Equal("en", resumed.Model!.Culture);
        Assert.Equal("English exam", resumed.Model.ExamName);
        Assert.Equal("English description", resumed.Model.ExamDescription);
        Assert.Equal("English question", Assert.Single(resumed.Model.Questions).Description);
        Assert.Equal("Answer", Assert.Single(resumed.Model.Questions[0].Choices).ChoiceText);

        var result = await SubmitTestHelpers.SubmitAsync(db, started.AttemptId, null, started.AccessToken);

        Assert.Equal("en", result!.Culture);
        Assert.Equal("English exam", result.ExamName);
    }

    [Fact]
    public async Task StartSnapshotsQuestionsAndResumeUsesTheSnapshot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var examId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var choiceId = Guid.NewGuid();
        var otherChoiceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using var db = new ApplicationDbContext(options);
        var exam = new Exam
        {
            Id = examId,
            Name = "Published exam",
            Description = "Exam description",
            Code = "EX",
            Slug = "published-exam",
            Status = Status.Published,
            PassingPercentage = 70,
            DurationMinutes = 30,
            CreatedOnUtc = now,
            Skills = [],
            Questions =
            [
                new Question
                {
                    Id = questionId,
                    ExamId = examId,
                    Description = "Original question text",
                    QuestionType = QuestionType.SingleChoice,
                    Status = Status.Published,
                    CreatedOnUtc = now,
                    Choices =
                    [
                        new Choice { Id = choiceId, ChoiceText = "Correct", IsCorrect = true, CreatedOnUtc = now },
                        new Choice { Id = otherChoiceId, ChoiceText = "Other", CreatedOnUtc = now }
                    ]
                }
            ]
        };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();

        Assert.Null(await StartTestHelpers.StartAsync(db, exam.Slug!, email: "invalid-email"));
        var started = await StartTestHelpers.StartAsync(db, exam.Slug!, email: " Candidate-1@Example.com ");

        Assert.NotNull(started);
        var snapshot = await db.Set<ExamAttemptQuestion>().Include(item => item.Choices).SingleAsync();
        Assert.Equal("Original question text", snapshot.QuestionTextSnapshot);
        Assert.Single(snapshot.Choices, item => item.IsCorrectSnapshot);
        Assert.NotEqual(started!.AccessToken, (await db.Set<ExamAttempt>().SingleAsync()).AnonymousTokenHash);
        var attemptRecord = await db.Set<ExamAttempt>().SingleAsync();
        Assert.Null(attemptRecord.UserId);
        Assert.Equal("candidate-1@example.com", attemptRecord.GuestEmail);

        var correctSnapshotChoice = snapshot.Choices.Single(item => item.SourceChoiceId == choiceId);
        var otherSnapshotChoice = snapshot.Choices.Single(item => item.SourceChoiceId == otherChoiceId);
        var saved = await AutosaveTestHelpers.SaveAsync(db, new SaveExamAnswerRequest
        {
            AttemptId = started.AttemptId,
            AttemptQuestionId = snapshot.Id,
            SelectedChoiceIds = [correctSnapshotChoice.Id]
        }, null, started.AccessToken);
        var replaced = await AutosaveTestHelpers.SaveAsync(db, new SaveExamAnswerRequest
        {
            AttemptId = started.AttemptId,
            AttemptQuestionId = snapshot.Id,
            SelectedChoiceIds = [otherSnapshotChoice.Id]
        }, null, started.AccessToken);
        Assert.Equal(SaveAnswerStatus.Saved, saved.Status);
        Assert.Equal(SaveAnswerStatus.Saved, replaced.Status);
        Assert.Single(db.Set<ExamAnswer>());

        exam.Questions![0].Description = "Changed after start";
        await db.SaveChangesAsync();

        var denied = await ReadTestHelpers.ResumeAsync(db, started.AttemptId, null, "wrong-token");
        var resumed = await ReadTestHelpers.ResumeAsync(db, started.AttemptId, null, started.AccessToken);

        Assert.Equal(ResumeAttemptStatus.NotFoundOrUnauthorized, denied.Status);
        Assert.Equal(ResumeAttemptStatus.Ready, resumed.Status);
        Assert.Equal("Original question text", Assert.Single(resumed.Model!.Questions).Description);
        Assert.Equal([otherChoiceId], resumed.Model.Questions[0].SelectedChoiceIds);
        Assert.Equal(started.AttemptId, resumed.Model.AttemptId);
        Assert.True(resumed.Model.RemainingSeconds > 0);
    }

    [Fact]
    public async Task StartRejectsUnpublishedExam()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        db.Set<Exam>().Add(new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Draft",
            Description = "Not public",
            Code = "DR",
            Slug = "draft",
            Status = Status.Draft,
            CreatedOnUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var started = await StartTestHelpers.StartAsync(db, "draft");

        Assert.Null(started);
        Assert.Empty(db.Set<ExamAttempt>());
    }

    [Fact]
    public async Task SaveAnswerRejectsExpiredAttempt()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = DateTime.UtcNow;
        await using var db = new ApplicationDbContext(options);
        var exam = new Exam
        {
            Id = Guid.NewGuid(), Name = "Timed", Description = "Timed exam", Code = "TM", Slug = "timed",
            Status = Status.Published, DurationMinutes = 15, PassingPercentage = 70, CreatedOnUtc = now,
            Questions = [new Question
            {
                Id = Guid.NewGuid(), Description = "Question", QuestionType = QuestionType.SingleChoice,
                Status = Status.Published, CreatedOnUtc = now,
                Choices = [new Choice { Id = Guid.NewGuid(), ChoiceText = "A", IsCorrect = true, CreatedOnUtc = now }, new Choice { Id = Guid.NewGuid(), ChoiceText = "B", CreatedOnUtc = now }]
            }]
        };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();
        var started = await StartTestHelpers.StartAsync(db, exam.Slug!);
        var attempt = await db.Set<ExamAttempt>().SingleAsync();
        var question = await db.Set<ExamAttemptQuestion>().Include(item => item.Choices).SingleAsync();
        var correctChoice = question.Choices.Single(choice => choice.IsCorrectSnapshot);
        var answerBeforeExpiry = await AutosaveTestHelpers.SaveAsync(db, new SaveExamAnswerRequest
        {
            AttemptId = started!.AttemptId,
            AttemptQuestionId = question.Id,
            SelectedChoiceIds = [correctChoice.Id]
        }, null, started.AccessToken);
        Assert.Equal(SaveAnswerStatus.Saved, answerBeforeExpiry.Status);
        attempt.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var saved = await AutosaveTestHelpers.SaveAsync(db, new SaveExamAnswerRequest
        {
            AttemptId = started.AttemptId,
            AttemptQuestionId = question.Id
        }, null, started.AccessToken);
        var result = await SubmitTestHelpers.SubmitAsync(db, started.AttemptId, null, started.AccessToken);

        Assert.Equal(SaveAnswerStatus.AttemptClosed, saved.Status);
        Assert.Equal(ExamAttemptStatus.Expired, (await db.Set<ExamAttempt>().SingleAsync()).Status);
        Assert.Equal(1m, result!.Score);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task SubmitScoresOnlySnapshottedQuestionsAndReturnsThePersistedResultAgain()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = DateTime.UtcNow;
        var correctSourceChoiceId = Guid.NewGuid();
        await using var db = new ApplicationDbContext(options);
        var exam = new Exam
        {
            Id = Guid.NewGuid(), Name = "Score exam", Description = "Description", Code = "SC", Slug = "score-exam",
            Status = Status.Published, DurationMinutes = 20, PassingPercentage = 70, CreatedOnUtc = now,
            Questions =
            [
                new Question
                {
                    Id = Guid.NewGuid(), Description = "First", QuestionType = QuestionType.SingleChoice,
                    Status = Status.Published, CreatedOnUtc = now,
                    Choices = [new Choice { Id = correctSourceChoiceId, ChoiceText = "Right", IsCorrect = true, CreatedOnUtc = now }, new Choice { Id = Guid.NewGuid(), ChoiceText = "Wrong", CreatedOnUtc = now }]
                },
                new Question
                {
                    Id = Guid.NewGuid(), Description = "Second", QuestionType = QuestionType.TrueFalse,
                    Status = Status.Published, CreatedOnUtc = now,
                    Choices = [new Choice { Id = Guid.NewGuid(), ChoiceText = "True", IsCorrect = true, CreatedOnUtc = now }, new Choice { Id = Guid.NewGuid(), ChoiceText = "False", CreatedOnUtc = now }]
                }
            ]
        };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();
        var started = await StartTestHelpers.StartAsync(db, exam.Slug!);
        var firstQuestion = await db.Set<ExamAttemptQuestion>().Include(item => item.Choices)
            .FirstAsync(item => item.Choices.Any(choice => choice.SourceChoiceId == correctSourceChoiceId));
        var answerChoice = firstQuestion.Choices.Single(choice => choice.SourceChoiceId == correctSourceChoiceId);
        var answerSave = await AutosaveTestHelpers.SaveAsync(db, new SaveExamAnswerRequest
        {
            AttemptId = started!.AttemptId,
            AttemptQuestionId = firstQuestion.Id,
            SelectedChoiceIds = [answerChoice.Id]
        }, null, started.AccessToken);
        Assert.Equal(SaveAnswerStatus.Saved, answerSave.Status);

        var submitted = await SubmitTestHelpers.SubmitAsync(db, started.AttemptId, null, started.AccessToken);
        var sourceQuestion = exam.Questions!.Single(question => question.Description == "First");
        sourceQuestion.Description = "Question edited after submission";
        sourceQuestion.Choices!.Single(choice => choice.Id == correctSourceChoiceId).IsCorrect = false;
        await db.SaveChangesAsync();
        var repeated = await SubmitTestHelpers.SubmitAsync(db, started.AttemptId, null, started.AccessToken);
        var resultPage = await ReadTestHelpers.GetResultAsync(db, started.AttemptId, null, started.AccessToken);

        Assert.Equal(ExamAttemptStatus.Completed, (await db.Set<ExamAttempt>().SingleAsync()).Status);
        Assert.Equal(1m, submitted!.Score);
        Assert.Equal(2m, submitted.MaxScore);
        Assert.Equal(50, submitted.Percentage);
        Assert.False(submitted.Passed);
        Assert.Equal(submitted.Score, repeated!.Score);
        Assert.Equal(submitted.Percentage, resultPage!.Percentage);
        Assert.Equal("First", resultPage.Questions.Single(question => question.QuestionId == sourceQuestion.Id).Description);
        Assert.False(resultPage.Questions.Single(question => question.Description == "Second").IsCorrect);
    }
}
