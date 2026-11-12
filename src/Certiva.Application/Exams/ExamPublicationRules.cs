using Certiva.Domain.Enums;
using Certiva.Domain.Exams;

namespace Certiva.Application.Exams;

public static class ExamPublicationRules
{
    private static readonly HashSet<QuestionType> SupportedTypes =
    [QuestionType.SingleChoice, QuestionType.MultiChoice, QuestionType.TrueFalse, QuestionType.ShortAnswer];

    public static bool IsPublishable(Exam exam)
    {
        if (string.IsNullOrWhiteSpace(exam.Name) || string.IsNullOrWhiteSpace(exam.Description) ||
            string.IsNullOrWhiteSpace(exam.Code) || string.IsNullOrWhiteSpace(exam.Slug) ||
            exam.DurationMinutes is < 1 or > 1440 || exam.PassingPercentage is < 0 or > 100)
            return false;

        var questions = exam.Questions?.Where(q => q.Status == Status.Published).ToList() ?? [];
        if (questions.Count == 0) return false;

        // Starting an exam is allowed as soon as it has one published question.
        // Draft or incomplete questions are not a reason to block the whole exam.
        return true;
    }
}
