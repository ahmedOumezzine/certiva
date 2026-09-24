using Certiva.Domain.Entities;
using Certiva.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Certiva.Domain.Exams
{
    public class Exam : BaseEntity
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        [MaxLength(6)]
        public string? Code { get; set; }

        [Required]
        public string? Slug { get; set; }

        public int QuestionsCount { get; set; }
        [Range(1, 1440)]
        public int? DurationMinutes { get; set; }

        [Range(0, 100)]
        public int PassingPercentage { get; set; } = 70;

        public Status Status { get; set; }

        public virtual List<Skill>? Skills { get; set; }
        public virtual List<Question>? Questions { get; set; }
        public virtual List<ExamTranslation> Translations { get; set; } = new();
    }
}
