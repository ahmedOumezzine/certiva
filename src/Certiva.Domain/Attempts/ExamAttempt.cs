using Certiva.Domain.Entities;
using Certiva.Domain.Exams;
using Certiva.Domain.Enums;

namespace Certiva.Domain.Attempts;

public class ExamAttempt : BaseEntity
{
    public Guid ExamId { get; set; }
    public string ExamNameSnapshot { get; set; } = string.Empty;
    public string ExamDescriptionSnapshot { get; set; } = string.Empty;
    public string ExamSlugSnapshot { get; set; } = string.Empty;
    public string Culture { get; set; } = "fr";
    public int PassingPercentageSnapshot { get; set; } = 70;
    public string? UserId { get; set; }
    public string? GuestEmail { get; set; }
    public string? AnonymousTokenHash { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public virtual Exam? Exam { get; set; }
    public virtual List<ExamAttemptQuestion> Questions { get; set; } = [];
}
