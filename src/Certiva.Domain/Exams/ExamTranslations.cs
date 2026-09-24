using Certiva.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Certiva.Domain.Exams;

public interface IExamContentTranslation
{
    string Culture { get; set; }
}

public sealed class ExamTranslation : BaseEntity, IExamContentTranslation
{
    [Required, StringLength(2, MinimumLength = 2)]
    public string Culture { get; set; } = "fr";
    public Guid ExamId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public Exam Exam { get; set; } = null!;
}

public sealed class SkillTranslation : BaseEntity, IExamContentTranslation
{
    [Required, StringLength(2, MinimumLength = 2)]
    public string Culture { get; set; } = "fr";
    public Guid SkillId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Skill Skill { get; set; } = null!;
}

public sealed class QuestionTranslation : BaseEntity, IExamContentTranslation
{
    [Required, StringLength(2, MinimumLength = 2)]
    public string Culture { get; set; } = "fr";
    public Guid QuestionId { get; set; }
    public string? Description { get; set; }
    public string? Explanation { get; set; }
    public string? FillInBlank { get; set; }
    public Question Question { get; set; } = null!;
}

public sealed class ChoiceTranslation : BaseEntity, IExamContentTranslation
{
    [Required, StringLength(2, MinimumLength = 2)]
    public string Culture { get; set; } = "fr";
    public Guid ChoiceId { get; set; }
    public string? ChoiceText { get; set; }
    public string? GroupBy { get; set; }
    public Choice Choice { get; set; } = null!;
}
