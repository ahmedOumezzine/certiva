using Certiva.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Certiva.Domain.Exams
{
    public class Choice : BaseEntity
    {
        public string? ChoiceText { get; set; }
        public bool IsCorrect { get; set; }
        public string? GroupBy { get; set; }

        public Guid? QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        [JsonIgnore] // ✅ Empêche la boucle
        public virtual Question? Question { get; set; }

        public virtual List<ChoiceTranslation> Translations { get; set; } = new();
    }
}
