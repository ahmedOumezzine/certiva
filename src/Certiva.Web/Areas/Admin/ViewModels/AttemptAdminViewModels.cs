using Certiva.Domain.Enums;

namespace Certiva.Areas.Admin.ViewModels;

public sealed class AttemptIndexViewModel
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public ExamAttemptStatus? Status { get; set; }
    public Guid? ExamId { get; set; }
    public List<AttemptRowViewModel> Attempts { get; set; } = [];
}

public sealed class AttemptRowViewModel
{
    public Guid Id { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
    public string? CandidateEmail { get; set; }
    public bool IsGuest { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ExamAttemptStatus Status { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
}

public sealed class AttemptDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public string ExamSlug { get; set; } = string.Empty;
    public bool ExamIsPublic { get; set; }
    public string Candidate { get; set; } = string.Empty;
    public string? CandidateEmail { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    public ExamAttemptStatus Status { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
    public int PassingPercentage { get; set; }
    public List<AttemptQuestionAdminViewModel> Questions { get; set; } = [];
    public List<AttemptSkillScoreViewModel> SkillScores { get; set; } = [];
}

public sealed class AttemptQuestionAdminViewModel
{
    public int Order { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? SkillName { get; set; }
    public string? Explanation { get; set; }
    public decimal Points { get; set; }
    public decimal ScoreAwarded { get; set; }
    public bool? IsCorrect { get; set; }
    public bool HasAnswer { get; set; }
    public string? TextAnswer { get; set; }
    public List<AttemptChoiceAdminViewModel> Choices { get; set; } = [];
}

public sealed class AttemptSkillScoreViewModel
{
    public string SkillName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Percentage => Total == 0 ? 0 : (int)Math.Round(Correct * 100m / Total);
}

public sealed class AttemptChoiceAdminViewModel
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool WasSelected { get; set; }
}
