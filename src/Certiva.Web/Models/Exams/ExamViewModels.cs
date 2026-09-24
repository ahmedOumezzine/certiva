using Certiva.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Certiva.Localization;

namespace Certiva.Models.Exams;

public sealed class ExamListViewModel
{
    public string Culture { get; set; } = "fr";
    public string Search { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<ExamCardViewModel> Exams { get; set; } = new();
    public List<SkillFilterViewModel> Skills { get; set; } = new();
}

public sealed class ExamCardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription => Description.Length <= 150 ? Description : Description[..147] + "...";
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SourceSlug { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public Status Status { get; set; }
    public int? DurationMinutes { get; set; }
    public int PassingPercentage { get; set; }
    public List<string> Skills { get; set; } = new();
    public bool HasEnglishFallback { get; set; }
}

public sealed class SkillFilterViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool HasEnglishFallback { get; set; }
}

public sealed class ExamDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SourceSlug { get; set; } = string.Empty;
    public bool HasCompleteTranslation { get; set; }
    public bool HasCompleteEnglishTranslation { get; set; }
    public bool HasEnglishFallback { get; set; }
    public string FrenchSlug { get; set; } = string.Empty;
    public string EnglishSlug { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int EstimatedMinutes { get; set; }
    public int? DurationMinutes { get; set; }
    public int PassingPercentage { get; set; }
    public Status Status { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
    public bool CanStart { get; set; }
    public List<string> PublicationErrors { get; set; } = new();
    public List<ExamSkillViewModel> Skills { get; set; } = new();
    public List<QuestionTypeCountViewModel> QuestionTypes { get; set; } = new();
    public List<ExamQuestionPreviewViewModel> Questions { get; set; } = new();
}

public sealed class ExamSkillViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Pourcentage { get; set; }
    public int QuestionsCount { get; set; }
}

public sealed class QuestionTypeCountViewModel
{
    public QuestionType Type { get; set; }
    public int Count { get; set; }
}

public sealed class ExamQuestionPreviewViewModel
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string SkillName { get; set; } = "Général";
}

public sealed class TakeExamViewModel
{
    public string Culture { get; set; } = "fr";
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string ExamDescription { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public int PassingPercentage { get; set; }
    public string ExamSlug { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public string Mode { get; set; } = "all";
    public bool Random { get; set; }
    public int EstimatedMinutes { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
    public List<TakeQuestionViewModel> Questions { get; set; } = new();
}

public sealed class StartExamAttemptRequest
{
    [Required(ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "LoginEmailRequired")]
    [EmailAddress(ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "ValidEmailRequired")]
    [StringLength(256, ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "GuestEmailTooLong")]
    public string GuestEmail { get; set; } = string.Empty;
    [Required]
    public string ExamSlug { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public bool Random { get; set; }
}

public sealed class TakeQuestionViewModel
{
    public Guid Id { get; set; }
    public Guid AttemptQuestionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FillInBlank { get; set; }
    public QuestionType QuestionType { get; set; }
    public string SkillName { get; set; } = "General";
    public List<Guid> SelectedChoiceIds { get; set; } = new();
    public string? TextAnswer { get; set; }
    public List<TakeChoiceViewModel> Choices { get; set; } = new();
}

public sealed class TakeChoiceViewModel
{
    public Guid Id { get; set; }
    public Guid AttemptChoiceId { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public string? GroupBy { get; set; }
}

public sealed class SubmitExamViewModel
{
    [Required]
    public string ExamSlug { get; set; } = string.Empty;

    public Guid? SkillId { get; set; }
    public string Mode { get; set; } = "all";
    public bool Random { get; set; }
    public int DurationSeconds { get; set; }
}

public sealed class ExamResultViewModel
{
    public string Culture { get; set; } = "fr";
    public string ExamName { get; set; } = string.Empty;
    public string ExamSlug { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers => Math.Max(0, TotalQuestions - CorrectAnswers);
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public int Percentage { get; set; }
    public bool Passed { get; set; }
    public int PassingScore { get; set; } = 70;
    public int DurationSeconds { get; set; }
    public List<SkillResultViewModel> SkillResults { get; set; } = new();
    public List<QuestionReviewViewModel> Questions { get; set; } = new();
}

public sealed class SkillResultViewModel
{
    public string SkillName { get; set; } = "General";
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Percentage => Total == 0 ? 0 : (int)Math.Round(Correct * 100m / Total);
}

public sealed class QuestionReviewViewModel
{
    public Guid QuestionId { get; set; }
    public Guid AttemptQuestionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SkillName { get; set; } = "General";
    public QuestionType QuestionType { get; set; }
    public bool IsCorrect { get; set; }
    public decimal Points { get; set; }
    public decimal ScoreAwarded { get; set; }
    public string? Explication { get; set; }
    public List<string> UserAnswers { get; set; } = new();
    public List<string> CorrectAnswers { get; set; } = new();
}
