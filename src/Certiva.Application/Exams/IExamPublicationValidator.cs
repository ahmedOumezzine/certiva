using Certiva.Domain.Exams;

namespace Certiva.Application.Exams;

public interface IExamPublicationValidator
{
    IReadOnlyList<string> Validate(Exam exam);
}
