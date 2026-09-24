using Certiva.Domain.Entities;
using Certiva.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Certiva.Domain.Exams
{
    public class Question : BaseEntity
    {
        public string? Description { get; set; }
        public string? Explication { get; set; }
        public string? FillInBlank { get; set; }
        public QuestionType QuestionType { get; set; }
        public Status Status { get; set; }

        public Guid? ExamId { get; set; }

        [ForeignKey("ExamId")]
        [JsonIgnore] // ✅ Empêche la boucle
        public virtual Exam? Exam { get; set; }

        public Guid? SkillId { get; set; }
        [JsonIgnore] // ✅ Empêche la boucle
        public virtual Skill? Skill { get; set; }

        public virtual List<Choice>? Choices { get; set; }
        public virtual List<QuestionTranslation> Translations { get; set; } = new();
    }
}
