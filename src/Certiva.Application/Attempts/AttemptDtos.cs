using Certiva.Domain.Enums;

namespace Certiva.Application.Attempts;

public sealed record AttemptSessionDto(
    Guid AttemptId,
    Guid ExamId,
    string Culture,
    string ExamName,
    string ExamDescription,
    string ExamCode,
    string ExamSlug,
    int PassingPercentage,
    int? DurationMinutes,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc,
    int RemainingSeconds,
    IReadOnlyList<AttemptQuestionDto> Questions);

public sealed record AttemptQuestionDto(
    Guid QuestionId,
    Guid AttemptQuestionId,
    int Order,
    string Description,
    QuestionType QuestionType,
    string SkillName,
    IReadOnlyList<Guid> SelectedChoiceIds,
    string? TextAnswer,
    IReadOnlyList<AttemptChoiceDto> Choices);

public sealed record AttemptChoiceDto(
    Guid ChoiceId,
    Guid AttemptChoiceId,
    int Order,
    string ChoiceText,
    string? GroupBy);

public sealed record AttemptResultDto(
    string Culture,
    string ExamName,
    string ExamSlug,
    int TotalQuestions,
    int CorrectAnswers,
    decimal Score,
    decimal MaxScore,
    int Percentage,
    bool Passed,
    int PassingScore,
    int DurationSeconds,
    IReadOnlyList<SkillResultDto> SkillResults,
    IReadOnlyList<AttemptQuestionResultDto> Questions,
    ExamAttemptStatus Status = ExamAttemptStatus.Completed);

public sealed record AttemptQuestionResultDto(
    Guid QuestionId,
    Guid AttemptQuestionId,
    string Description,
    string SkillName,
    QuestionType QuestionType,
    bool IsCorrect,
    decimal Points,
    decimal ScoreAwarded,
    string? Explanation,
    IReadOnlyList<string> UserAnswers,
    IReadOnlyList<string> CorrectAnswers);

public sealed record SkillResultDto(
    string SkillName,
    int Total,
    int Correct);

public sealed record StartAttemptRequest(
    string Slug,
    Guid? SkillId,
    bool Random,
    string GuestEmail,
    string Culture = "fr");

public sealed record StartedAttemptDto(Guid AttemptId, string AccessToken);

public sealed record SaveAnswerRequest(
    Guid AttemptId,
    Guid AttemptQuestionId,
    IReadOnlyList<Guid> SelectedChoiceIds,
    string? TextAnswer);

public enum ResumeAttemptResultStatus
{
    Ready,
    NotFoundOrUnauthorized,
    Expired
}

public sealed record ResumeAttemptResultDto(
    ResumeAttemptResultStatus Status,
    AttemptSessionDto? Session = null);

public enum SaveAnswerResultStatus
{
    Saved,
    NotFoundOrUnauthorized,
    InvalidAnswer,
    AttemptClosed
}

public sealed record SaveAnswerResultDto(
    SaveAnswerResultStatus Status,
    DateTime? SavedAtUtc = null);
