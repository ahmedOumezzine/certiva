using Certiva.Domain.Entities;
namespace Certiva.Domain.Attempts;

public class ExamAttemptChoice : BaseEntity
{
    public Guid ExamAttemptQuestionId { get; set; }
    public Guid SourceChoiceId { get; set; }
    public int Order { get; set; }
    public string ChoiceTextSnapshot { get; set; } = string.Empty;
    public string? GroupBySnapshot { get; set; }
    public bool IsCorrectSnapshot { get; set; }
    public virtual ExamAttemptQuestion? ExamAttemptQuestion { get; set; }
}
