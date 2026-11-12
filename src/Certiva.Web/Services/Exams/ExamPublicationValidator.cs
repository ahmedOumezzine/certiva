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

        return errors;
    }
}
