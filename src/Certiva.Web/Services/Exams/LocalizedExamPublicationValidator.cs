using Certiva.Application.Exams;
using Certiva.Domain.Exams;
using Certiva.Localization;
using Microsoft.Extensions.Localization;

namespace Certiva.Services.Exams;

public sealed class LocalizedExamPublicationValidator : IExamPublicationValidator
{
    private readonly IStringLocalizer<SharedResource> _text;

    public LocalizedExamPublicationValidator(IStringLocalizer<SharedResource> text) => _text = text;

    public IReadOnlyList<string> Validate(Exam exam) =>
        ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, exam.DurationMinutes, exam.PassingPercentage, _text);
}
