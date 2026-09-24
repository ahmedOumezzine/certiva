namespace Certiva.Application.Exams;

public interface IExamCatalogService
{
    Task<ExamCatalogPageDto> GetExamListAsync(string? search, Guid? skillId, int page, int pageSize, string culture = "fr", CancellationToken cancellationToken = default);
    Task<ExamDetailsDto?> GetDetailsAsync(string slug, string culture = "fr", CancellationToken cancellationToken = default);
    Task<bool> HasCompleteEnglishCatalogAsync(CancellationToken cancellationToken = default);
}
