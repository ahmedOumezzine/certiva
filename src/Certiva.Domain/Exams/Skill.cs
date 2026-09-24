using Certiva.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Certiva.Domain.Exams
{
    public class Skill : BaseEntity
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public int? Pourcentage { get; set; }

        public Guid? ExamId { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }
        public virtual List<SkillTranslation> Translations { get; set; } = new();
    }
}
