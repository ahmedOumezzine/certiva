using Certiva.Models.Exams;

namespace Certiva.Services.Exams;

public enum ResumeAttemptStatus { Ready, NotFoundOrUnauthorized, Expired }
public sealed record ResumedExamAttempt(ResumeAttemptStatus Status, TakeExamViewModel? Model = null);
public enum SaveAnswerStatus { Saved, NotFoundOrUnauthorized, InvalidAnswer, AttemptClosed }
public sealed record SaveAnswerResult(SaveAnswerStatus Status, DateTime? SavedAtUtc = null);
public sealed class SaveExamAnswerRequest
{
    public Guid AttemptId { get; set; }
    public Guid AttemptQuestionId { get; set; }
    public List<Guid> SelectedChoiceIds { get; set; } = [];
    public string? TextAnswer { get; set; }
}
