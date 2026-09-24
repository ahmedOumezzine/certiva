using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Areas.Admin.Services;
using Certiva.Areas.Admin.ViewModels;
using Certiva.Infrastructure.Data;
using Certiva.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class AdminAttemptServiceTests
{
    [Fact]
    public async Task ListsAttemptsByFilterAndPageInDescendingStartTimeAndProjectsUserDetailsOnce()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var examId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        await using var db = new ApplicationDbContext(options);
        db.Users.Add(new ApplicationUser { Id = userId, FullName = "Candidate Name", Email = "candidate@example.com" });
        db.Set<ExamAttempt>().AddRange(Enumerable.Range(0, 30).Select(index => new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            ExamNameSnapshot = "Filtered exam",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-index),
            Status = ExamAttemptStatus.Completed,
            UserId = index == 0 ? userId : null,
            GuestEmail = index == 0 ? null : $"guest{index}@example.com",
            CreatedOnUtc = DateTime.UtcNow
        }).Concat(
        [new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = Guid.NewGuid(),
            ExamNameSnapshot = "Other exam",
            StartedAtUtc = DateTime.UtcNow.AddDays(1),
            Status = ExamAttemptStatus.Completed,
            CreatedOnUtc = DateTime.UtcNow
        }]));
        await db.SaveChangesAsync();

        IStringLocalizerFactory localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance);
        var service = new AdminAttemptService(db, new StringLocalizer<SharedResource>(localizerFactory));
        var firstPage = await service.GetAttemptsAsync(1, ExamAttemptStatus.Completed, examId);
        var secondPage = await service.GetAttemptsAsync(2, ExamAttemptStatus.Completed, examId);

        Assert.Equal(30, firstPage.TotalCount);
        Assert.Equal(25, firstPage.Attempts.Count);
        Assert.Equal(5, secondPage.Attempts.Count);
        Assert.True(firstPage.Attempts[0].StartedAtUtc > firstPage.Attempts[1].StartedAtUtc);
        Assert.Equal("Candidate Name", firstPage.Attempts[0].Candidate);
        Assert.Equal("candidate@example.com", firstPage.Attempts[0].CandidateEmail);
        Assert.All(firstPage.Attempts, attempt => Assert.Equal("Filtered exam", attempt.ExamName));
    }

    [Theory]
    [InlineData("fr-FR", "Invité")]
    [InlineData("en-US", "Guest")]
    public async Task ListsAnonymousAttemptsAndShowsStoredCorrectionFromSnapshot(string cultureName, string expectedGuestLabel)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerChoiceId = Guid.NewGuid();
        await using var db = new ApplicationDbContext(options);
        db.Set<Exam>().Add(new Exam { Id = examId, Name = "Exam", Description = "Description", Code = "EX", Slug = "exam", CreatedOnUtc = DateTime.UtcNow });
        db.Set<ExamAttempt>().Add(new ExamAttempt
        {
            Id = attemptId,
            ExamId = examId,
            ExamNameSnapshot = "Snapshot exam name",
            ExamSlugSnapshot = "snapshot-exam",
            PassingPercentageSnapshot = 70,
            GuestEmail = "guest@example.com",
            AnonymousTokenHash = "secret-hash-must-not-be-returned",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-3),
            CompletedAtUtc = DateTime.UtcNow,
            Status = ExamAttemptStatus.Completed,
            Score = 1m,
            MaxScore = 1m,
            Percentage = 100m,
            Passed = true,
            CreatedOnUtc = DateTime.UtcNow,
            Questions =
            [
                new ExamAttemptQuestion
                {
                    Id = questionId,
                    SourceQuestionId = Guid.NewGuid(),
                    Order = 1,
                    Points = 1m,
                    QuestionTextSnapshot = "Frozen prompt",
                    QuestionTypeSnapshot = QuestionType.SingleChoice,
                    CreatedOnUtc = DateTime.UtcNow,
                    Choices =
                    [
                        new ExamAttemptChoice { Id = answerChoiceId, Order = 1, ChoiceTextSnapshot = "Frozen correct", IsCorrectSnapshot = true, CreatedOnUtc = DateTime.UtcNow },
                        new ExamAttemptChoice { Id = Guid.NewGuid(), Order = 2, ChoiceTextSnapshot = "Frozen wrong", CreatedOnUtc = DateTime.UtcNow }
                    ],
                    Answer = new ExamAnswer
                    {
                        Id = Guid.NewGuid(),
                        SelectedChoiceIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { answerChoiceId }),
                        SavedAtUtc = DateTime.UtcNow,
                        IsCorrect = true,
                        ScoreAwarded = 1m,
                        CreatedOnUtc = DateTime.UtcNow
                    }
                }
            ]
        });
        await db.SaveChangesAsync();
        IStringLocalizerFactory localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance);
        var text = new StringLocalizer<SharedResource>(localizerFactory);
        var service = new AdminAttemptService(db, text);

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        AttemptIndexViewModel list;
        AttemptDetailsViewModel? detail;
        try
        {
            Assert.Equal(expectedGuestLabel, text["Guest"].Value);
            list = await service.GetAttemptsAsync(1, ExamAttemptStatus.Completed, examId);
            detail = await service.GetAttemptAsync(attemptId);
            Assert.Equal(cultureName, CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        Assert.Single(list.Attempts);
        Assert.Equal(expectedGuestLabel, list.Attempts[0].Candidate);
        Assert.Equal("guest@example.com", list.Attempts[0].CandidateEmail);
        Assert.True(list.Attempts[0].IsGuest);
        Assert.Equal("Snapshot exam name", list.Attempts[0].ExamName);
        Assert.NotNull(detail);
        Assert.Equal(expectedGuestLabel, detail!.Candidate);
        Assert.Equal("guest@example.com", detail.CandidateEmail);
        Assert.Equal("Frozen prompt", Assert.Single(detail!.Questions).QuestionText);
        Assert.True(detail.Questions[0].Choices.Single(choice => choice.IsCorrect).WasSelected);
    }

    [Fact]
    public async Task DetailsKeepsSnapshotOrderStoredScoreAndSkillSummary()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var selectedId = Guid.NewGuid();
        await using var db = new ApplicationDbContext(options);
        db.Set<Exam>().Add(new Exam
        {
            Id = examId,
            Name = "Changed live exam title",
            Description = "Current description",
            Code = "NEW",
            Slug = "new-live-slug",
            Status = Status.Published,
            CreatedOnUtc = DateTime.UtcNow
        });
        db.Set<ExamAttempt>().Add(new ExamAttempt
        {
            Id = attemptId,
            ExamId = examId,
            ExamNameSnapshot = "Frozen exam",
            ExamSlugSnapshot = "frozen-exam",
            Score = 2m,
            MaxScore = 3m,
            Percentage = 66.67m,
            Passed = false,
            Status = ExamAttemptStatus.Completed,
            CreatedOnUtc = DateTime.UtcNow,
            Questions =
            [
                new ExamAttemptQuestion
                {
                    Id = Guid.NewGuid(),
                    Order = 2,
                    Points = 2m,
                    QuestionTextSnapshot = "Second frozen question",
                    SkillNameSnapshot = "Databases",
                    CreatedOnUtc = DateTime.UtcNow,
                    Choices = [new ExamAttemptChoice { Id = selectedId, Order = 1, ChoiceTextSnapshot = "Frozen answer", IsCorrectSnapshot = true, CreatedOnUtc = DateTime.UtcNow }],
                    Answer = new ExamAnswer { Id = Guid.NewGuid(), SelectedChoiceIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { selectedId }), IsCorrect = true, ScoreAwarded = 2m, SavedAtUtc = DateTime.UtcNow, CreatedOnUtc = DateTime.UtcNow }
                },
                new ExamAttemptQuestion
                {
                    Id = Guid.NewGuid(),
                    Order = 1,
                    Points = 1m,
                    QuestionTextSnapshot = "First frozen question",
                    SkillNameSnapshot = "Databases",
                    CreatedOnUtc = DateTime.UtcNow,
                    Choices = [new ExamAttemptChoice { Id = Guid.NewGuid(), Order = 1, ChoiceTextSnapshot = "Expected", IsCorrectSnapshot = true, CreatedOnUtc = DateTime.UtcNow }],
                    Answer = new ExamAnswer { Id = Guid.NewGuid(), TextAnswer = "Candidate response", IsCorrect = false, ScoreAwarded = 0m, SavedAtUtc = DateTime.UtcNow, CreatedOnUtc = DateTime.UtcNow }
                }
            ]
        });
        await db.SaveChangesAsync();

        IStringLocalizerFactory localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance);
        var service = new AdminAttemptService(db, new StringLocalizer<SharedResource>(localizerFactory));
        var detail = await service.GetAttemptAsync(attemptId);

        Assert.NotNull(detail);
        Assert.Equal("Frozen exam", detail!.ExamName);
        Assert.Equal("NEW", detail.ExamCode);
        Assert.Equal("new-live-slug", detail.ExamSlug);
        Assert.True(detail.ExamIsPublic);
        Assert.Equal(2m, detail.Score);
        Assert.Equal(3m, detail.MaxScore);
        Assert.Equal(["First frozen question", "Second frozen question"], detail.Questions.Select(question => question.QuestionText));
        Assert.Equal("Candidate response", detail.Questions[0].TextAnswer);
        Assert.True(detail.Questions[1].Choices.Single().WasSelected);
        var skill = Assert.Single(detail.SkillScores);
        Assert.Equal("Databases", skill.SkillName);
        Assert.Equal(2, skill.Total);
        Assert.Equal(1, skill.Correct);
        Assert.Equal(50, skill.Percentage);
    }
}
