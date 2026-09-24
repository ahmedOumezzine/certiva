using Certiva.Application.Exams;
using Certiva.Infrastructure.Data;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Infrastructure.Data.Queries;

public sealed class EfExamCatalogReader : IExamCatalogReader
{
    private readonly ApplicationDbContext _db;
    private readonly IExamPublicationValidator? _validator;
    public EfExamCatalogReader(ApplicationDbContext db, IExamPublicationValidator? validator = null) { _db = db; _validator = validator; }

    public async Task<ExamCatalogPageDto> GetCatalogAsync(string? search, Guid? skillId, int page, int pageSize, string culture = "fr", CancellationToken cancellationToken = default)
    {
        culture = NormalizeCulture(culture); search = (search ?? string.Empty).Trim(); var normalizedSearch = search.ToLowerInvariant(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 24);
        var query = _db.Set<Exam>().AsNoTracking().Where(e => e.Status == Status.Published);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(e => (culture == "en" ? e.Translations!.Any(t => t.Culture == "en" && t.Name != null && t.Name.ToLower().Contains(normalizedSearch)) || (!e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name)) && e.Name != null && e.Name.ToLower().Contains(normalizedSearch)) : e.Name != null && e.Name.ToLower().Contains(normalizedSearch)) || (e.Code != null && e.Code.ToLower().Contains(normalizedSearch)));
        if (skillId.HasValue && skillId.Value != Guid.Empty) query = query.Where(e => e.Skills != null && e.Skills.Any(s => s.Id == skillId.Value));
        var total = await query.CountAsync(cancellationToken);
        var exams = await query.OrderBy(e => culture == "en" ? e.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? e.Name : e.Name).Skip((page - 1) * pageSize).Take(pageSize).Select(e => new ExamCatalogItemDto(e.Id, culture == "en" ? e.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? e.Name ?? string.Empty : e.Name ?? string.Empty, culture == "en" ? e.Translations!.Where(t => t.Culture == "en").Select(t => t.Description).FirstOrDefault() ?? e.Description ?? string.Empty : e.Description ?? string.Empty, e.Code ?? string.Empty, culture == "en" ? e.Translations!.Where(t => t.Culture == "en").Select(t => t.Slug).FirstOrDefault() ?? e.Slug ?? string.Empty : e.Slug ?? string.Empty, e.Slug ?? string.Empty, e.Questions == null ? e.QuestionsCount : e.Questions.Count(q => q.Status == Status.Published), e.Status, e.DurationMinutes, e.PassingPercentage, e.Skills == null ? new List<string>() : e.Skills.OrderBy(s => culture == "en" ? s.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? s.Name : s.Name).Select(s => culture == "en" ? s.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? s.Name ?? string.Empty : s.Name ?? string.Empty).ToList(), culture == "en" && (!e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name)) || !e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Description)) || (e.Skills != null && e.Skills.Any(s => !string.IsNullOrEmpty(s.Name) && !s.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name))))))).ToListAsync(cancellationToken);
        var skills = await _db.Set<Skill>().AsNoTracking().Where(s => s.Exam != null && s.Exam.Status == Status.Published).OrderBy(s => culture == "en" ? s.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? s.Name : s.Name).Select(s => new ExamSkillFilterDto(s.Id, culture == "en" ? s.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? s.Name ?? string.Empty : s.Name ?? string.Empty, culture == "en" && !string.IsNullOrEmpty(s.Name) && !s.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name)))).ToListAsync(cancellationToken);
        return new ExamCatalogPageDto(culture, search, skillId, page, pageSize, total, exams, skills);
    }

    public async Task<ExamDetailsDto?> GetDetailsAsync(string slug, string culture = "fr", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null; culture = NormalizeCulture(culture);
        var query = _db.Set<Exam>().AsNoTracking().AsSplitQuery().Include(e => e.Translations).Include(e => e.Skills!).ThenInclude(s => s!.Translations).Include(e => e.Questions!).ThenInclude(q => q.Translations).Include(e => e.Questions!).ThenInclude(q => q!.Skill!).ThenInclude(s => s!.Translations);
        var exam = culture == "en" ? await query.FirstOrDefaultAsync(e => e.Status == Status.Published && e.Translations!.Any(t => t.Culture == "en" && t.Slug == slug), cancellationToken) ?? await query.FirstOrDefaultAsync(e => e.Status == Status.Published && e.Slug == slug, cancellationToken) : await query.FirstOrDefaultAsync(e => e.Status == Status.Published && e.Slug == slug, cancellationToken);
        return exam == null ? null : MapDetails(exam, culture);
    }

    public Task<bool> HasCompleteEnglishCatalogAsync(CancellationToken cancellationToken = default) => _db.Set<Exam>().AsNoTracking().Where(e => e.Status == Status.Published).AllAsync(e => e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name)) && e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Description)) && e.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Slug)) && e.Skills!.All(s => (string.IsNullOrEmpty(s.Name) || s.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Name))) && (string.IsNullOrEmpty(s.Description) || s.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Description)))) && e.Questions!.Where(q => q.Status == Status.Published).All(q => (string.IsNullOrEmpty(q.Description) || q.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.Description))) && q.Choices!.All(c => string.IsNullOrEmpty(c.ChoiceText) || c.Translations!.Any(t => t.Culture == "en" && !string.IsNullOrEmpty(t.ChoiceText)))), cancellationToken);

    private ExamDetailsDto MapDetails(Exam e, string culture)
    {
        var et = e.Translations?.FirstOrDefault(t => t.Culture == culture); var en = e.Translations?.FirstOrDefault(t => t.Culture == "en"); var all = e.Questions ?? []; var published = all.Where(q => q.Status == Status.Published).ToList(); var complete = !string.IsNullOrWhiteSpace(en?.Name) && !string.IsNullOrWhiteSpace(en.Description) && !string.IsNullOrWhiteSpace(en.Slug);
        var skills = (e.Skills ?? []).OrderBy(s => Localized(s.Translations?.FirstOrDefault(t => t.Culture == culture)?.Name, s.Name)).Select(s => new ExamSkillDto(s.Id, Localized(s.Translations?.FirstOrDefault(t => t.Culture == culture)?.Name, s.Name), Localized(s.Translations?.FirstOrDefault(t => t.Culture == culture)?.Description, s.Description), s.Pourcentage ?? 0, published.Count(q => q.SkillId == s.Id))).ToList();
        complete = complete && (e.Skills ?? []).All(s => (string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Translations?.FirstOrDefault(t => t.Culture == "en")?.Name)) && (string.IsNullOrWhiteSpace(s.Description) || !string.IsNullOrWhiteSpace(s.Translations?.FirstOrDefault(t => t.Culture == "en")?.Description))) && published.All(q => (string.IsNullOrWhiteSpace(q.Description) || !string.IsNullOrWhiteSpace(q.Translations?.FirstOrDefault(t => t.Culture == "en")?.Description)) && (q.Choices ?? []).All(c => string.IsNullOrWhiteSpace(c.ChoiceText) || !string.IsNullOrWhiteSpace(c.Translations?.FirstOrDefault(t => t.Culture == "en")?.ChoiceText)));
        var previews = published.OrderBy(q => q.CreatedOnUtc).ThenBy(q => q.Id).Select((q, i) => new QuestionSummaryDto(i + 1, Localized(q.Translations?.FirstOrDefault(t => t.Culture == culture)?.Description, q.Description), q.QuestionType, e.Skills?.FirstOrDefault(s => s.Id == q.SkillId) is { } skill ? Localized(skill.Translations?.FirstOrDefault(t => t.Culture == culture)?.Name, skill.Name) : culture == "en" ? "General" : "Général")).ToList();
        var errors = _validator?.Validate(e) ?? [];
        return new ExamDetailsDto(e.Id, Localized(et?.Name, e.Name), Localized(et?.Description, e.Description), e.Code ?? string.Empty, Localized(et?.Slug, e.Slug), e.Slug ?? string.Empty, culture == "fr" || complete, complete, culture == "en" && !complete, e.Slug ?? string.Empty, Localized(en?.Slug, e.Slug), published.Count, Math.Max(15, published.Count * 2), e.DurationMinutes, e.PassingPercentage, e.Status, e.LastModifiedOnUtc ?? e.CreatedOnUtc, errors.Count == 0, errors, skills, published.GroupBy(q => q.QuestionType).Select(g => new QuestionTypeCountDto(g.Key, g.Count())).OrderBy(x => x.Type).ToList(), previews);
    }
    private static string NormalizeCulture(string? culture) => culture == "en" ? "en" : "fr";
    private static string Localized(string? translated, string? fallback) => string.IsNullOrWhiteSpace(translated) ? fallback ?? string.Empty : translated;
}
