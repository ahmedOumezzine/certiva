using Certiva.Domain.Enums;

namespace Certiva.Application.Exams;

public sealed record ExamCatalogPageDto(
    string Culture,
    string Search,
    Guid? SkillId,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ExamCatalogItemDto> Exams,
    IReadOnlyList<ExamSkillFilterDto> Skills);

public sealed record ExamCatalogItemDto(
    Guid Id,
    string Name,
    string Description,
    string Code,
    string Slug,
    string SourceSlug,
    int QuestionsCount,
    Status Status,
    int? DurationMinutes,
    int PassingPercentage,
    IReadOnlyList<string> Skills,
    bool HasEnglishFallback);

public sealed record ExamSkillFilterDto(
    Guid Id,
    string Name,
    bool HasEnglishFallback);

public sealed record ExamDetailsDto(
    Guid Id,
    string Name,
    string Description,
    string Code,
    string Slug,
    string SourceSlug,
    bool HasCompleteTranslation,
    bool HasCompleteEnglishTranslation,
    bool HasEnglishFallback,
    string FrenchSlug,
    string EnglishSlug,
    int QuestionsCount,
    int EstimatedMinutes,
    int? DurationMinutes,
    int PassingPercentage,
    Status Status,
    DateTime? LastUpdatedUtc,
    bool CanStart,
    IReadOnlyList<string> PublicationErrors,
    IReadOnlyList<ExamSkillDto> Skills,
    IReadOnlyList<QuestionTypeCountDto> QuestionTypes,
    IReadOnlyList<QuestionSummaryDto> Questions);

public sealed record ExamSkillDto(
    Guid Id,
    string Name,
    string Description,
    int Percentage,
    int QuestionsCount);

public sealed record QuestionSummaryDto(
    int Number,
    string Text,
    QuestionType Type,
    string SkillName);

public sealed record QuestionTypeCountDto(QuestionType Type, int Count);
