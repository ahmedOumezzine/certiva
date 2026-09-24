using Certiva.Application.Exams;

namespace Certiva.Models.Exams;

public static class ExamCatalogViewModelMapper
{
    public static ExamListViewModel ToViewModel(this ExamCatalogPageDto dto) => new()
    {
        Culture = dto.Culture, Search = dto.Search, SkillId = dto.SkillId, Page = dto.Page, PageSize = dto.PageSize, TotalCount = dto.TotalCount,
        Exams = dto.Exams.Select(item => new ExamCardViewModel { Id = item.Id, Name = item.Name, Description = item.Description, Code = item.Code, Slug = item.Slug, SourceSlug = item.SourceSlug, QuestionsCount = item.QuestionsCount, Status = item.Status, DurationMinutes = item.DurationMinutes, PassingPercentage = item.PassingPercentage, Skills = item.Skills.ToList(), HasEnglishFallback = item.HasEnglishFallback }).ToList(),
        Skills = dto.Skills.Select(item => new SkillFilterViewModel { Id = item.Id, Name = item.Name, HasEnglishFallback = item.HasEnglishFallback }).ToList()
    };

    public static ExamDetailsViewModel ToViewModel(this ExamDetailsDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name, Description = dto.Description, Code = dto.Code, Slug = dto.Slug, SourceSlug = dto.SourceSlug,
        HasCompleteTranslation = dto.HasCompleteTranslation, HasCompleteEnglishTranslation = dto.HasCompleteEnglishTranslation, HasEnglishFallback = dto.HasEnglishFallback,
        FrenchSlug = dto.FrenchSlug, EnglishSlug = dto.EnglishSlug, QuestionsCount = dto.QuestionsCount, EstimatedMinutes = dto.EstimatedMinutes,
        DurationMinutes = dto.DurationMinutes, PassingPercentage = dto.PassingPercentage, Status = dto.Status, LastUpdatedUtc = dto.LastUpdatedUtc,
        CanStart = dto.CanStart, PublicationErrors = dto.PublicationErrors.ToList(),
        Skills = dto.Skills.Select(item => new ExamSkillViewModel { Id = item.Id, Name = item.Name, Description = item.Description, Pourcentage = item.Percentage, QuestionsCount = item.QuestionsCount }).ToList(),
        QuestionTypes = dto.QuestionTypes.Select(item => new QuestionTypeCountViewModel { Type = item.Type, Count = item.Count }).ToList(),
        Questions = dto.Questions.Select(item => new ExamQuestionPreviewViewModel { Number = item.Number, Text = item.Text, Type = item.Type, SkillName = item.SkillName }).ToList()
    };
}
