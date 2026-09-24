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

        foreach (var question in questions)
        {
            if (question.ExamId != exam.Id || string.IsNullOrWhiteSpace(question.Description) || !SupportedTypes.Contains(question.QuestionType)) return false;
            if (question.SkillId.HasValue && !(exam.Skills?.Any(s => s.Id == question.SkillId.Value && s.ExamId == exam.Id) ?? false)) return false;

            var choices = question.Choices ?? [];
            if (choices.Any(c => string.IsNullOrWhiteSpace(c.ChoiceText))) return false;
            var correct = choices.Count(c => c.IsCorrect);
            if (question.QuestionType == QuestionType.SingleChoice && (choices.Count < 2 || correct != 1)) return false;
            if (question.QuestionType == QuestionType.MultiChoice && (choices.Count < 2 || correct == 0)) return false;
            if (question.QuestionType == QuestionType.TrueFalse && (choices.Count != 2 || choices.Count(c => string.Equals(c.ChoiceText?.Trim(), "True", StringComparison.OrdinalIgnoreCase)) != 1 || choices.Count(c => string.Equals(c.ChoiceText?.Trim(), "False", StringComparison.OrdinalIgnoreCase)) != 1 || correct != 1)) return false;
            if (question.QuestionType == QuestionType.ShortAnswer && !choices.Any(c => c.IsCorrect && !string.IsNullOrWhiteSpace(c.ChoiceText))) return false;
        }

        return true;
    }
}
