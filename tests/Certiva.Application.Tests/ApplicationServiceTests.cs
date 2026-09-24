using Certiva.Application.Attempts;
using Certiva.Application.Exams;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Xunit;

namespace Certiva.Application.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task ReadServiceDelegatesResumeAndResultWithoutChangingArguments()
    {
        var reader = new FakeAttemptReader();
        var service = new ExamAttemptReadService(reader);
        var attemptId = Guid.NewGuid();

        var resume = await service.ResumeAsync(attemptId, "user-1", "token");
        var result = await service.GetResultAsync(attemptId, "user-1", "token");

        Assert.Same(reader.ResumeResult, resume);
        Assert.Same(reader.Result, result);
        Assert.Equal((attemptId, "user-1", "token"), reader.LastReadArguments);
    }

    [Fact]
    public async Task AutosaveServiceDelegatesRequestAndCancellation()
    {
        var store = new FakeAnswerStore();
        var service = new ExamAttemptAutosaveService(store);
        using var cancellation = new CancellationTokenSource();
        var request = new SaveAnswerRequest(Guid.NewGuid(), Guid.NewGuid(), [], "answer");

        var result = await service.SaveAnswerAsync(request, "user-1", "token", cancellation.Token);

        Assert.Same(store.Result, result);
        Assert.Equal(request, store.Request);
        Assert.Equal("user-1", store.UserId);
        Assert.Equal("token", store.AccessToken);
        Assert.Equal(cancellation.Token, store.CancellationToken);
    }

    [Fact]
    public async Task CatalogServiceDelegatesAllReaderOperations()
    {
        var reader = new FakeCatalogReader();
        var service = new ExamCatalogService(reader);
        var skillId = Guid.NewGuid();

        var page = await service.GetExamListAsync("search", skillId, 2, 10, "en");
        var details = await service.GetDetailsAsync("slug", "en");
        var hasEnglish = await service.HasCompleteEnglishCatalogAsync();

        Assert.Same(reader.Page, page);
        Assert.Same(reader.Details, details);
        Assert.True(hasEnglish);
        Assert.Equal(("search", skillId, 2, 10, "en"), reader.ListArguments);
        Assert.Equal(("slug", "en"), reader.DetailsArguments);
    }

    [Fact]
    public void PublicationRulesRejectMissingExamData()
    {
        Assert.False(ExamPublicationRules.IsPublishable(new Exam()));
    }

    [Fact]
    public void PublicationRulesAcceptAValidSingleChoiceExam()
    {
        var exam = PublishableExam();
        var question = new Question
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Description = "Question",
            Status = Status.Published,
            QuestionType = QuestionType.SingleChoice,
            Choices =
            [
                new Choice { ChoiceText = "Correct", IsCorrect = true },
                new Choice { ChoiceText = "Wrong" },
            ],
        };
        exam.Questions = [question];

        Assert.True(ExamPublicationRules.IsPublishable(exam));
    }

    [Fact]
    public void PublicationRulesRejectUnsupportedQuestionType()
    {
        var exam = PublishableExam();
        exam.Questions = [new Question
        {
            Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Question",
            Status = Status.Published, QuestionType = QuestionType.LongAnswer,
        }];

        Assert.False(ExamPublicationRules.IsPublishable(exam));
    }

    [Fact]
    public void PublicationRulesRejectInvalidChoiceCountsAndCorrectAnswers()
    {
        var exam = PublishableExam();
        exam.Questions = [new Question
        {
            Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Question",
            Status = Status.Published, QuestionType = QuestionType.SingleChoice,
            Choices = [new Choice { ChoiceText = "Only choice", IsCorrect = true }],
        }];

        Assert.False(ExamPublicationRules.IsPublishable(exam));
    }

    [Fact]
    public void PublicationRulesValidateTrueFalseLabelsAndCorrectAnswer()
    {
        var exam = PublishableExam();
        exam.Questions = [new Question
        {
            Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Question",
            Status = Status.Published, QuestionType = QuestionType.TrueFalse,
            Choices =
            [
                new Choice { ChoiceText = "True", IsCorrect = true },
                new Choice { ChoiceText = "False" },
            ],
        }];

        Assert.True(ExamPublicationRules.IsPublishable(exam));
    }

    private static Exam PublishableExam() => new()
    {
        Id = Guid.NewGuid(), Name = "Exam", Description = "Description", Code = "CERT",
        Slug = "exam", DurationMinutes = 30, PassingPercentage = 70,
    };

    private sealed class FakeAttemptReader : IExamAttemptReader
    {
        public ResumeAttemptResultDto ResumeResult { get; } = new(ResumeAttemptResultStatus.Ready);
        public AttemptResultDto Result { get; } = new("fr", "Exam", "exam", 1, 1, 1, 1, 100, true, 70, 10, [], []);
        public (Guid, string?, string?) LastReadArguments { get; private set; }
        public Task<ResumeAttemptResultDto> ResumeAsync(Guid id, string? userId, string? token, CancellationToken ct = default)
        { LastReadArguments = (id, userId, token); return Task.FromResult(ResumeResult); }
        public Task<AttemptResultDto?> GetResultAsync(Guid id, string? userId, string? token, CancellationToken ct = default)
        { LastReadArguments = (id, userId, token); return Task.FromResult<AttemptResultDto?>(Result); }
    }

    private sealed class FakeAnswerStore : IExamAttemptAnswerStore
    {
        public SaveAnswerResultDto Result { get; } = new(SaveAnswerResultStatus.Saved);
        public SaveAnswerRequest? Request { get; private set; }
        public string? UserId { get; private set; }
        public string? AccessToken { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public Task<SaveAnswerResultDto> SaveAnswerAsync(SaveAnswerRequest request, string? userId, string? token, CancellationToken ct = default)
        { Request = request; UserId = userId; AccessToken = token; CancellationToken = ct; return Task.FromResult(Result); }
    }

    private sealed class FakeCatalogReader : IExamCatalogReader
    {
        public ExamCatalogPageDto Page { get; } = new("en", "", null, 1, 10, 0, [], []);
        public ExamDetailsDto Details { get; } = new(Guid.NewGuid(), "Exam", "Description", "CERT", "exam", "exam", true, true, false, "exam", "exam", 0, 0, null, 70, Status.Published, null, true, [], [], [], []);
        public (string?, Guid?, int, int, string) ListArguments { get; private set; }
        public (string, string) DetailsArguments { get; private set; }
        public Task<ExamCatalogPageDto> GetCatalogAsync(string? search, Guid? skillId, int page, int pageSize, string culture = "fr", CancellationToken ct = default)
        { ListArguments = (search, skillId, page, pageSize, culture); return Task.FromResult(Page); }
        public Task<ExamDetailsDto?> GetDetailsAsync(string slug, string culture = "fr", CancellationToken ct = default)
        { DetailsArguments = (slug, culture); return Task.FromResult<ExamDetailsDto?>(Details); }
        public Task<bool> HasCompleteEnglishCatalogAsync(CancellationToken ct = default) => Task.FromResult(true);
    }
}
