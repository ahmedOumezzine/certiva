using Certiva.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Certiva.Areas.Admin.ViewModels;

public sealed class ExamDashboardViewModel
{
    public int ExamsCount { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ArchivedCount { get; set; }
    public int QuestionsCount { get; set; }
    public int SkillsCount { get; set; }
    public List<ExamRowViewModel> RecentExams { get; set; } = new();
    public List<SkillQuestionCountViewModel> QuestionsBySkill { get; set; } = new();
}

public sealed class SkillQuestionCountViewModel
{
    public string SkillName { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
}

public sealed class ExamIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public Status? Status { get; set; }
    public List<ExamRowViewModel> Exams { get; set; } = new();
    public AdminPaginationViewModel Pagination { get; set; } = new();
}

public sealed class AdminPaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = AdminPagination.DefaultPageSize;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string Controller { get; set; } = string.Empty;
    public Dictionary<string, string> RouteValues { get; set; } = new();
}

public static class AdminPagination
{
    public const int DefaultPageSize = 25;

    public static int NormalizePageSize(int pageSize) => pageSize is 25 or 50 or 100
        ? pageSize
        : DefaultPageSize;

    public static int NormalizePage(int page) => Math.Max(1, page);

    public static int ClampPage(int page, int totalPages) =>
        totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
}

public sealed class ExamRowViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Status Status { get; set; }
    public int QuestionsCount { get; set; }
    public int SkillsCount { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class ExamEditViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(160)]
    public string EnglishName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string EnglishDescription { get; set; } = string.Empty;

    [StringLength(180)]
    public string EnglishSlug { get; set; } = string.Empty;

    [Range(0, 10000)]
    public int QuestionsCount { get; set; }

    public int SkillsCount { get; set; }

    [Range(1, 1440)]
    public int? DurationMinutes { get; set; }

    [Range(0, 100)]
    public int PassingPercentage { get; set; } = 70;

    public Status Status { get; set; } = Status.Draft;

    public DateTime CreatedOnUtc { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    public List<ExamQuestionTypeCountViewModel> QuestionTypeCounts { get; set; } = new();
}

public sealed class ExamQuestionTypeCountViewModel
{
    public QuestionType Type { get; set; }
    public int Count { get; set; }
}

public sealed class SkillIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public Guid? ExamId { get; set; }
    public List<ExamSelectItemViewModel> Exams { get; set; } = new();
    public List<SkillRowViewModel> Skills { get; set; } = new();
    public AdminPaginationViewModel Pagination { get; set; } = new();
}

public sealed class SkillRowViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public int Pourcentage { get; set; }
    public int QuestionsCount { get; set; }
}

public class SkillEditViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(140)]
    public string EnglishName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string EnglishDescription { get; set; } = string.Empty;

    [Range(0, 100)]
    public int Pourcentage { get; set; }

    [Required]
    public Guid ExamId { get; set; }

    public List<ExamSelectItemViewModel> Exams { get; set; } = new();
}

public sealed class QuestionIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public Guid? ExamId { get; set; }
    public Guid? SkillId { get; set; }
    public QuestionType? QuestionType { get; set; }
    public Status? Status { get; set; }
    public List<ExamSelectItemViewModel> Exams { get; set; } = new();
    public List<SkillSelectItemViewModel> Skills { get; set; } = new();
    public List<QuestionRowViewModel> Questions { get; set; } = new();
    public AdminPaginationViewModel Pagination { get; set; } = new();
}

public sealed class QuestionRowViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public Status Status { get; set; }
    public int ChoicesCount { get; set; }
}

public sealed class QuestionDetailsViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EnglishDescription { get; set; } = string.Empty;
    public string? Explication { get; set; }
    public string? EnglishExplication { get; set; }
    public QuestionType QuestionType { get; set; }
    public Status Status { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public List<QuestionChoiceDetailsViewModel> Choices { get; set; } = [];
}

public sealed class QuestionChoiceDetailsViewModel
{
    public string ChoiceText { get; set; } = string.Empty;
    public string EnglishChoiceText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class QuestionEditViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(4000)]
    public string EnglishDescription { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Explication { get; set; }

    [StringLength(4000)]
    public string? EnglishExplication { get; set; }

    [StringLength(1000)]
    public string? FillInBlank { get; set; }

    [StringLength(1000)]
    public string? EnglishFillInBlank { get; set; }

    public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;
    public Status Status { get; set; } = Status.Draft;

    [Required]
    public Guid ExamId { get; set; }

    public Guid? SkillId { get; set; }
    public List<ChoiceEditViewModel> Choices { get; set; } = new();
    public string? ExpectedAnswer { get; set; }
    public string? AcceptedAnswers { get; set; }
    public string? EnglishExpectedAnswer { get; set; }
    public string? EnglishAcceptedAnswers { get; set; }
    public bool CaseSensitive { get; set; }
    public List<DragDropPairViewModel> DragDropPairs { get; set; } = new();
    public List<GroupedChoiceViewModel> GroupedChoices { get; set; } = new();
    public List<ExamSelectItemViewModel> Exams { get; set; } = new();
    public List<SkillSelectItemViewModel> Skills { get; set; } = new();
}

public class ChoiceEditViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(1000)]
    public string ChoiceText { get; set; } = string.Empty;

    [StringLength(1000)]
    public string EnglishChoiceText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
    public string? GroupBy { get; set; }

    [StringLength(200)]
    public string? EnglishGroupBy { get; set; }
}

public sealed class ExamSelectItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SkillSelectItemViewModel
{
    public Guid Id { get; set; }
    public Guid? ExamId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DragDropPairViewModel
{
    public Guid Id { get; set; }

    [StringLength(1000)]
    public string Source { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Target { get; set; } = string.Empty;

    [StringLength(1000)]
    public string EnglishSource { get; set; } = string.Empty;

    [StringLength(1000)]
    public string EnglishTarget { get; set; } = string.Empty;
}

public class GroupedChoiceViewModel
{
    public Guid Id { get; set; }

    [StringLength(1000)]
    public string Item { get; set; } = string.Empty;

    [StringLength(200)]
    public string Group { get; set; } = string.Empty;

    [StringLength(1000)]
    public string EnglishItem { get; set; } = string.Empty;

    [StringLength(200)]
    public string EnglishGroup { get; set; } = string.Empty;
}
