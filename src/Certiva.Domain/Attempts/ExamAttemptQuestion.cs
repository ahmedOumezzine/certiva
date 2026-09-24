using Certiva.Domain.Entities;
using Certiva.Domain.Enums;

namespace Certiva.Domain.Attempts;

public class ExamAttemptQuestion : BaseEntity
{
    public Guid ExamAttemptId { get; set; }
    public Guid SourceQuestionId { get; set; }
    public int Order { get; set; }
    public decimal Points { get; set; } = 1m;
    public string QuestionTextSnapshot { get; set; } = string.Empty;
    public string? ExplanationSnapshot { get; set; }
    public QuestionType QuestionTypeSnapshot { get; set; }
    public string? SkillNameSnapshot { get; set; }
    public virtual ExamAttempt? ExamAttempt { get; set; }
    public virtual List<ExamAttemptChoice> Choices { get; set; } = [];
    public virtual ExamAnswer? Answer { get; set; }
}
