using Certiva.Domain.Entities;
namespace Certiva.Domain.Attempts;

public class ExamAnswer : BaseEntity
{
    public Guid ExamAttemptQuestionId { get; set; }
    public string? SelectedChoiceIdsJson { get; set; }
    public string? TextAnswer { get; set; }
    public DateTime SavedAtUtc { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal ScoreAwarded { get; set; }
    public virtual ExamAttemptQuestion? ExamAttemptQuestion { get; set; }
}
