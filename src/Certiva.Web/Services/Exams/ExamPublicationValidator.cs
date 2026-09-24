using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Certiva.Services.Exams;

public static class ExamPublicationValidator
{
    private static readonly HashSet<QuestionType> SupportedTypes =
    [
        QuestionType.SingleChoice,
        QuestionType.MultiChoice,
        QuestionType.TrueFalse,
        QuestionType.ShortAnswer
    ];

    public static IReadOnlyList<string> Validate(
        Exam exam,
        string? name,
        string? description,
        string? code,
        string? slug,
        int? durationMinutes,
        int passingPercentage,
        IStringLocalizer<SharedResource>? text = null)
    {
        var errors = new List<string>();
        string Localized(string key, string fallback, params object[] arguments) =>
            text == null ? string.Format(CultureInfo.CurrentCulture, fallback, arguments) : text[key, arguments].Value;

        if (string.IsNullOrWhiteSpace(name)) errors.Add(Localized("Publish.NameRequired", "Le nom de l’examen est obligatoire."));
        if (string.IsNullOrWhiteSpace(description)) errors.Add(Localized("Publish.DescriptionRequired", "La description de l’examen est obligatoire."));
        if (string.IsNullOrWhiteSpace(code)) errors.Add(Localized("Publish.CodeRequired", "Le code de l’examen est obligatoire."));
        if (string.IsNullOrWhiteSpace(slug)) errors.Add(Localized("Publish.SlugRequired", "Le slug de l’examen est obligatoire."));
        if (durationMinutes is < 1 or > 1440) errors.Add(Localized("Publish.DurationRange", "La durée doit être comprise entre 1 et 1440 minutes."));
        if (passingPercentage is < 0 or > 100) errors.Add(Localized("Publish.PassingRange", "Le seuil de réussite doit être compris entre 0 et 100%."));

        var questions = exam.Questions?.Where(question => question.Status == Status.Published).ToList() ?? new List<Question>();
        if (questions.Count == 0)
            errors.Add(Localized("Publish.QuestionRequired", "Publiez au moins une question utilisable avant de publier l’examen."));

        foreach (var question in questions)
        {
            var label = string.IsNullOrWhiteSpace(question.Description)
                ? text?["Publish.QuestionWithoutPrompt"].Value ?? "Question sans énoncé"
                : question.Description.Trim();
            if (question.ExamId != exam.Id)
                errors.Add(Localized("Publish.QuestionWrongExam", "« {0} » n’est pas rattachée à cet examen.", label));

            if (string.IsNullOrWhiteSpace(question.Description))
                errors.Add(Localized("Publish.QuestionDescriptionRequired", "Chaque question publiée doit avoir un énoncé."));

            if (!SupportedTypes.Contains(question.QuestionType))
            {
                errors.Add(Localized("Publish.UnsupportedType", "« {0} » utilise le type {1}, non pris en charge pour les examens notés.", label, text?.EnumLabel(question.QuestionType) ?? question.QuestionType.ToString()));
                continue;
            }

            if (question.SkillId.HasValue && !(exam.Skills?.Any(skill => skill.Id == question.SkillId.Value && skill.ExamId == exam.Id) ?? false))
                errors.Add(Localized("Publish.SkillWrongExam", "La compétence de « {0} » n’appartient pas à cet examen.", label));

            var choices = question.Choices ?? new List<Choice>();
            if (choices.Any(choice => string.IsNullOrWhiteSpace(choice.ChoiceText)))
                errors.Add(Localized("Publish.ChoiceTextRequired", "Tous les choix de « {0} » doivent avoir un texte.", label));

            var correctChoices = choices.Where(choice => choice.IsCorrect).ToList();
            switch (question.QuestionType)
            {
                case QuestionType.SingleChoice:
                    if (choices.Count < 2) errors.Add(Localized("Publish.TwoChoicesRequired", "« {0} » doit avoir au moins deux choix.", label));
                    if (correctChoices.Count != 1) errors.Add(Localized("Publish.ExactlyOneCorrectRequired", "« {0} » doit avoir exactement une bonne réponse.", label));
                    break;
                case QuestionType.MultiChoice:
                    if (choices.Count < 2) errors.Add(Localized("Publish.TwoChoicesRequired", "« {0} » doit avoir au moins deux choix.", label));
                    if (correctChoices.Count == 0) errors.Add(Localized("Publish.CorrectRequired", "« {0} » doit avoir au moins une bonne réponse.", label));
                    break;
                case QuestionType.TrueFalse:
                    if (choices.Count != 2 ||
                        choices.Count(choice => string.Equals(choice.ChoiceText?.Trim(), "True", StringComparison.OrdinalIgnoreCase)) != 1 ||
                        choices.Count(choice => string.Equals(choice.ChoiceText?.Trim(), "False", StringComparison.OrdinalIgnoreCase)) != 1)
                    {
                        errors.Add(Localized("Publish.TrueFalseChoicesRequired", "« {0} » doit contenir exactement les choix True et False.", label));
                    }
                    if (correctChoices.Count != 1) errors.Add(Localized("Publish.ExactlyOneCorrectRequired", "« {0} » doit avoir exactement une bonne réponse.", label));
                    break;
                case QuestionType.ShortAnswer:
                    if (!correctChoices.Any(choice => !string.IsNullOrWhiteSpace(choice.ChoiceText)))
                        errors.Add(Localized("Publish.ExpectedAnswerRequired", "« {0} » doit avoir une réponse attendue.", label));
                    break;
            }
        }

        return errors;
    }
}
