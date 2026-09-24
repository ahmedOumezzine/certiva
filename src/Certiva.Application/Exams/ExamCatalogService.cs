namespace Certiva.Application.Exams;

public sealed class ExamCatalogService : IExamCatalogService
{
    private readonly IExamCatalogReader _reader;

    public ExamCatalogService(IExamCatalogReader reader) => _reader = reader;

    public Task<ExamCatalogPageDto> GetExamListAsync(string? search, Guid? skillId, int page, int pageSize, string culture = "fr", CancellationToken cancellationToken = default) =>
        _reader.GetCatalogAsync(search, skillId, page, pageSize, culture, cancellationToken);

    public Task<ExamDetailsDto?> GetDetailsAsync(string slug, string culture = "fr", CancellationToken cancellationToken = default) =>
        _reader.GetDetailsAsync(slug, culture, cancellationToken);

    public Task<bool> HasCompleteEnglishCatalogAsync(CancellationToken cancellationToken = default) =>
        _reader.HasCompleteEnglishCatalogAsync(cancellationToken);
}
