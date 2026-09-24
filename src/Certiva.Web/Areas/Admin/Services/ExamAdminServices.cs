using Certiva.Areas.Admin.ViewModels;
using Certiva.Infrastructure.Data;
using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Services.Exams;
using Microsoft.EntityFrameworkCore;
using Certiva.Localization;
using Microsoft.Extensions.Localization;

namespace Certiva.Areas.Admin.Services;

public interface IExamAdminService
{
    Task<ExamDashboardViewModel> GetDashboardAsync();
    Task<ExamIndexViewModel> GetExamsAsync(string? search, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize);
    Task<ExamEditViewModel?> GetExamEditAsync(Guid id);
    Task<ExamEditViewModel?> GetExamDetailsAsync(Guid id);
    Task<bool> SaveExamAsync(ExamEditViewModel model, ModelStateDictionaryProxy modelState);
    Task<bool> DeleteExamAsync(Guid id);
    Task<List<ExamSelectItemViewModel>> GetExamOptionsAsync();
}

public interface ISkillAdminService
{
    Task<SkillIndexViewModel> GetSkillsAsync(Guid? examId, string? search = null, int page = 1, int pageSize = AdminPagination.DefaultPageSize);
    Task<SkillEditViewModel> NewSkillAsync(Guid? examId);
    Task<SkillEditViewModel?> GetSkillAsync(Guid id);
    Task<bool> SaveSkillAsync(SkillEditViewModel model, ModelStateDictionaryProxy modelState);
    Task<bool> DeleteSkillAsync(Guid id);
    Task<List<SkillSelectItemViewModel>> GetSkillOptionsAsync(Guid? examId = null);
}

public interface IQuestionAdminService
{
    Task<QuestionIndexViewModel> GetQuestionsAsync(string? search, Guid? examId, Guid? skillId, QuestionType? type, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize);
    Task<QuestionEditViewModel> NewQuestionAsync(Guid? examId);
    Task<QuestionEditViewModel?> GetQuestionAsync(Guid id);
    Task<QuestionDetailsViewModel?> GetQuestionDetailsAsync(Guid id);
    Task<bool> SaveQuestionAsync(QuestionEditViewModel model, ModelStateDictionaryProxy modelState);
    Task<bool> DeleteQuestionAsync(Guid id);
}

public sealed class ModelStateDictionaryProxy
{
    private readonly Action<string, string> _addError;
    public bool HasErrors { get; private set; }

    public ModelStateDictionaryProxy(Action<string, string> addError)
    {
        _addError = addError;
    }

    public void Add(string key, string error)
    {
        HasErrors = true;
        _addError(key, error);
    }
}

internal static class ExamAdminTranslationMapper
{
    public static void SetExam(Exam exam, string culture, string name, string description, string slug)
    {
        var translation = exam.Translations.FirstOrDefault(item => item.Culture == culture);
        if (translation == null)
        {
            translation = new ExamTranslation { Culture = culture, CreatedOnUtc = DateTime.UtcNow };
            exam.Translations.Add(translation);
        }

        translation.Name = EmptyToNull(name);
        translation.Description = EmptyToNull(description);
        translation.Slug = EmptyToNull(slug);
        translation.LastModifiedOnUtc = DateTime.UtcNow;
    }

    public static void SetSkill(Skill skill, string culture, string name, string description)
    {
        var translation = skill.Translations.FirstOrDefault(item => item.Culture == culture);
        if (translation == null)
        {
            translation = new SkillTranslation { Culture = culture, CreatedOnUtc = DateTime.UtcNow };
            skill.Translations.Add(translation);
        }

        translation.Name = EmptyToNull(name);
        translation.Description = EmptyToNull(description);
        translation.LastModifiedOnUtc = DateTime.UtcNow;
    }

    public static void SetQuestion(Question question, string culture, string description, string? explanation, string? fillInBlank)
    {
        var translation = question.Translations.FirstOrDefault(item => item.Culture == culture);
        if (translation == null)
        {
            translation = new QuestionTranslation { Culture = culture, CreatedOnUtc = DateTime.UtcNow };
            question.Translations.Add(translation);
        }

        translation.Description = EmptyToNull(description);
        translation.Explanation = EmptyToNull(explanation);
        translation.FillInBlank = EmptyToNull(fillInBlank);
        translation.LastModifiedOnUtc = DateTime.UtcNow;
    }

    public static void SetChoice(Choice choice, string culture, string choiceText, string? groupBy)
    {
        var translation = choice.Translations.FirstOrDefault(item => item.Culture == culture);
        if (translation == null)
        {
            translation = new ChoiceTranslation { Culture = culture, CreatedOnUtc = DateTime.UtcNow };
            choice.Translations.Add(translation);
        }

        translation.ChoiceText = EmptyToNull(choiceText);
        translation.GroupBy = EmptyToNull(groupBy);
        translation.LastModifiedOnUtc = DateTime.UtcNow;
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ExamAdminService : IExamAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> _text;

    public ExamAdminService(ApplicationDbContext db, IStringLocalizer<SharedResource> text)
    {
        _db = db;
        _text = text;
    }

    public async Task<ExamDashboardViewModel> GetDashboardAsync()
    {
        var examStatusCounts = await _db.Set<Exam>().AsNoTracking()
            .GroupBy(exam => exam.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        var countsByStatus = examStatusCounts.ToDictionary(item => item.Status, item => item.Count);

        var questionsBySkill = await (
            from skill in _db.Set<Skill>().AsNoTracking()
            join question in _db.Set<Question>().AsNoTracking()
                on (Guid?)skill.Id equals question.SkillId
            group question by new { skill.Id, skill.Name } into skillQuestions
            orderby skillQuestions.Count() descending
            select new SkillQuestionCountViewModel
            {
                SkillName = skillQuestions.Key.Name ?? string.Empty,
                QuestionsCount = skillQuestions.Count()
            })
            .Take(8)
            .ToListAsync();

        return new ExamDashboardViewModel
        {
            ExamsCount = examStatusCounts.Sum(item => item.Count),
            PublishedCount = countsByStatus.GetValueOrDefault(Status.Published),
            DraftCount = countsByStatus.GetValueOrDefault(Status.Draft),
            ArchivedCount = countsByStatus.GetValueOrDefault(Status.Archived),
            QuestionsCount = await _db.Set<Question>().AsNoTracking().CountAsync(),
            SkillsCount = await _db.Set<Skill>().CountAsync(),
            QuestionsBySkill = questionsBySkill,
            RecentExams = await _db.Set<Exam>().AsNoTracking()
                .OrderByDescending(e => e.CreatedOnUtc)
                .Take(6)
                .Select(e => new ExamRowViewModel
                {
                    Id = e.Id,
                    Name = e.Name ?? string.Empty,
                    Code = e.Code ?? string.Empty,
                    Slug = e.Slug ?? string.Empty,
                    Status = e.Status,
                    CreatedOnUtc = e.CreatedOnUtc,
                    QuestionsCount = e.Questions == null ? e.QuestionsCount : e.Questions.Count,
                    SkillsCount = e.Skills == null ? 0 : e.Skills.Count
                })
                .ToListAsync()
        };
    }

    public async Task<ExamIndexViewModel> GetExamsAsync(string? search, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        search = (search ?? string.Empty).Trim();
        pageSize = AdminPagination.NormalizePageSize(pageSize);
        page = AdminPagination.NormalizePage(page);
        var query = _db.Set<Exam>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => (e.Name != null && e.Name.Contains(search)) || (e.Code != null && e.Code.Contains(search)));
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = AdminPagination.ClampPage(page, totalPages);

        return new ExamIndexViewModel
        {
            Search = search,
            Status = status,
            Pagination = new AdminPaginationViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Controller = "Exam",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search,
                    ["status"] = status?.ToString() ?? string.Empty,
                    ["pageSize"] = pageSize.ToString()
                }
            },
            Exams = await query.OrderBy(e => e.Name).ThenBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExamRowViewModel
            {
                Id = e.Id,
                Name = e.Name ?? string.Empty,
                Code = e.Code ?? string.Empty,
                Slug = e.Slug ?? string.Empty,
                Status = e.Status,
                QuestionsCount = e.Questions == null ? e.QuestionsCount : e.Questions.Count,
                SkillsCount = e.Skills == null ? 0 : e.Skills.Count
            }).ToListAsync()
        };
    }

    public Task<ExamEditViewModel?> GetExamEditAsync(Guid id) => GetExamAsync(id, includeQuestionTypeCounts: false);

    public Task<ExamEditViewModel?> GetExamDetailsAsync(Guid id) => GetExamAsync(id, includeQuestionTypeCounts: true);

    private async Task<ExamEditViewModel?> GetExamAsync(Guid id, bool includeQuestionTypeCounts)
    {
        var model = await _db.Set<Exam>().AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ExamEditViewModel
            {
                Id = e.Id,
                Name = e.Name ?? string.Empty,
                Description = e.Description ?? string.Empty,
                EnglishName = e.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                EnglishDescription = e.Translations!.Where(t => t.Culture == "en").Select(t => t.Description).FirstOrDefault() ?? string.Empty,
                EnglishSlug = e.Translations!.Where(t => t.Culture == "en").Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                Code = e.Code ?? string.Empty,
                Slug = e.Slug ?? string.Empty,
                QuestionsCount = e.Questions == null ? e.QuestionsCount : e.Questions.Count(question => question.Status == Status.Published),
                SkillsCount = e.Skills == null ? 0 : e.Skills.Count,
                DurationMinutes = e.DurationMinutes,
                PassingPercentage = e.PassingPercentage,
                Status = e.Status,
                CreatedOnUtc = e.CreatedOnUtc,
                LastModifiedOnUtc = e.LastModifiedOnUtc
            })
            .FirstOrDefaultAsync();

        if (model == null)
            return null;

        if (includeQuestionTypeCounts)
        {
            model.QuestionTypeCounts = await _db.Set<Question>().AsNoTracking()
                .Where(question => question.ExamId == id && question.Status == Status.Published)
                .GroupBy(question => question.QuestionType)
                .Select(group => new ExamQuestionTypeCountViewModel { Type = group.Key, Count = group.Count() })
                .OrderBy(item => item.Type)
                .ToListAsync();
        }

        return model;
    }

    public async Task<bool> SaveExamAsync(ExamEditViewModel model, ModelStateDictionaryProxy modelState)
    {
        model.Name = model.Name.Trim();
        model.Code = model.Code.Trim().ToUpperInvariant();
        model.Slug = ToSlug(model.Slug);
        model.Description = model.Description.Trim();

        if (await _db.Set<Exam>().AnyAsync(e => e.Slug == model.Slug && e.Id != model.Id))
            modelState.Add(nameof(model.Slug), _text["Exam.SlugInUse"]);
        if (await _db.Set<Exam>().AnyAsync(e => e.Code == model.Code && e.Id != model.Id))
            modelState.Add(nameof(model.Code), _text["Exam.CodeInUse"]);
        if (modelState.HasErrors)
            return false;

        model.EnglishName = model.EnglishName?.Trim() ?? string.Empty;
        model.EnglishDescription = model.EnglishDescription?.Trim() ?? string.Empty;
        model.EnglishSlug = string.IsNullOrWhiteSpace(model.EnglishSlug) ? string.Empty : ToSlug(model.EnglishSlug);
        if (!string.IsNullOrEmpty(model.EnglishSlug) && await _db.Set<ExamTranslation>()
                .AnyAsync(t => t.Culture == "en" && t.Slug == model.EnglishSlug && t.ExamId != model.Id))
            modelState.Add(nameof(model.EnglishSlug), _text["Exam.EnglishSlugInUse"]);
        if (modelState.HasErrors)
            return false;

        Exam? exam;
        if (model.Id == Guid.Empty)
        {
            exam = new Exam { CreatedOnUtc = DateTime.UtcNow, Skills = new List<Skill>(), Questions = new List<Question>(), Translations = new List<ExamTranslation>() };
        }
        else
        {
            var query = _db.Set<Exam>().Include(e => e.Translations).AsQueryable();
            if (model.Status == Status.Published)
                query = query.Include(e => e.Skills).Include(e => e.Questions!).ThenInclude(q => q.Choices);
            exam = await query.FirstOrDefaultAsync(e => e.Id == model.Id);
        }
        if (exam == null)
            return false;

        if (model.Status == Status.Published)
        {
            var validationErrors = ExamPublicationValidator.Validate(
                exam,
                model.Name,
                model.Description,
                model.Code,
                model.Slug,
                model.DurationMinutes,
                model.PassingPercentage,
                _text);
            foreach (var error in validationErrors)
                modelState.Add(nameof(model.Status), error);
            if (validationErrors.Count > 0)
                return false;
        }

        exam.Name = model.Name;
        exam.Description = model.Description;
        exam.Code = model.Code;
        exam.Slug = model.Slug;
        exam.QuestionsCount = model.QuestionsCount;
        exam.DurationMinutes = model.DurationMinutes;
        exam.PassingPercentage = model.PassingPercentage;
        exam.Status = model.Status;
        exam.LastModifiedOnUtc = DateTime.UtcNow;
        ExamAdminTranslationMapper.SetExam(exam, "fr", model.Name, model.Description, model.Slug);
        ExamAdminTranslationMapper.SetExam(exam, "en", model.EnglishName, model.EnglishDescription, model.EnglishSlug);

        if (model.Id == Guid.Empty)
            _db.Set<Exam>().Add(exam);

        await _db.SaveChangesAsync();
        model.Id = exam.Id;
        return true;
    }

    public async Task<bool> DeleteExamAsync(Guid id)
    {
        var exam = await _db.Set<Exam>().Include(e => e.Questions).Include(e => e.Skills).FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null)
            return false;

        var hasAttempts = await _db.Set<ExamAttempt>().AnyAsync(attempt => attempt.ExamId == id);
        if (hasAttempts || (exam.Questions?.Any() ?? false) || (exam.Skills?.Any() ?? false))
        {
            exam.Status = Status.Archived;
        }
        else
        {
            _db.Set<Exam>().Remove(exam);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ExamSelectItemViewModel>> GetExamOptionsAsync() =>
        await _db.Set<Exam>().AsNoTracking().OrderBy(e => e.Name)
            .Select(e => new ExamSelectItemViewModel { Id = e.Id, Name = e.Name ?? string.Empty })
            .ToListAsync();

    private static string ToSlug(string value) =>
        string.Join("-", (value ?? string.Empty).Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

public sealed class SkillAdminService : ISkillAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IExamAdminService _exams;
    private readonly IStringLocalizer<SharedResource> _text;

    public SkillAdminService(ApplicationDbContext db, IExamAdminService exams, IStringLocalizer<SharedResource> text)
    {
        _db = db;
        _exams = exams;
        _text = text;
    }

    public async Task<SkillIndexViewModel> GetSkillsAsync(Guid? examId, string? search = null, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        search = (search ?? string.Empty).Trim();
        pageSize = AdminPagination.NormalizePageSize(pageSize);
        page = AdminPagination.NormalizePage(page);
        var query = _db.Set<Skill>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(skill =>
                (skill.Name != null && skill.Name.Contains(search)) ||
                (skill.Exam != null && skill.Exam.Name != null && skill.Exam.Name.Contains(search)));
        if (examId.HasValue && examId.Value != Guid.Empty)
            query = query.Where(s => s.ExamId == examId.Value);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = AdminPagination.ClampPage(page, totalPages);

        return new SkillIndexViewModel
        {
            Search = search,
            ExamId = examId,
            Exams = await _exams.GetExamOptionsAsync(),
            Pagination = new AdminPaginationViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Controller = "Skill",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search,
                    ["examId"] = examId?.ToString() ?? string.Empty,
                    ["pageSize"] = pageSize.ToString()
                }
            },
            Skills = await query.OrderBy(s => s.Exam!.Name).ThenBy(s => s.Name).ThenBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SkillRowViewModel
                {
                    Id = s.Id,
                    Name = s.Name ?? string.Empty,
                    ExamName = s.Exam == null ? string.Empty : s.Exam.Name ?? string.Empty,
                    Pourcentage = s.Pourcentage ?? 0,
                    QuestionsCount = _db.Set<Question>().Count(q => q.SkillId == s.Id)
                }).ToListAsync()
        };
    }

    public async Task<SkillEditViewModel> NewSkillAsync(Guid? examId) =>
        new() { ExamId = examId ?? Guid.Empty, Exams = await _exams.GetExamOptionsAsync() };

    public async Task<SkillEditViewModel?> GetSkillAsync(Guid id)
    {
        var model = await _db.Set<Skill>().AsNoTracking().Where(s => s.Id == id)
            .Select(s => new SkillEditViewModel
            {
                Id = s.Id,
                Name = s.Name ?? string.Empty,
                Description = s.Description ?? string.Empty,
                EnglishName = s.Translations!.Where(t => t.Culture == "en").Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                EnglishDescription = s.Translations!.Where(t => t.Culture == "en").Select(t => t.Description).FirstOrDefault() ?? string.Empty,
                Pourcentage = s.Pourcentage ?? 0,
                ExamId = s.ExamId ?? Guid.Empty
            }).FirstOrDefaultAsync();
        if (model != null)
            model.Exams = await _exams.GetExamOptionsAsync();
        return model;
    }

    public async Task<bool> SaveSkillAsync(SkillEditViewModel model, ModelStateDictionaryProxy modelState)
    {
        var examExists = model.ExamId != Guid.Empty && await _db.Set<Exam>().AnyAsync(e => e.Id == model.ExamId);
        if (!examExists)
            modelState.Add(nameof(model.ExamId), _text["Admin.ExistingExamRequired"]);

        var otherTotal = await _db.Set<Skill>()
            .Where(s => s.ExamId == model.ExamId && s.Id != model.Id)
            .SumAsync(s => s.Pourcentage ?? 0);
        if (otherTotal + model.Pourcentage > 100)
            modelState.Add(nameof(model.Pourcentage), _text["Skill.WeightTotalExceeded"]);
        if (modelState.HasErrors)
            return false;

        model.Name = model.Name.Trim();
        model.Description = model.Description.Trim();
        model.EnglishName = model.EnglishName?.Trim() ?? string.Empty;
        model.EnglishDescription = model.EnglishDescription?.Trim() ?? string.Empty;
        var skill = model.Id == Guid.Empty
            ? new Skill { CreatedOnUtc = DateTime.UtcNow, Translations = new List<SkillTranslation>() }
            : await _db.Set<Skill>().Include(s => s.Translations).FirstOrDefaultAsync(s => s.Id == model.Id);
        if (skill == null)
            return false;

        skill.Name = model.Name.Trim();
        skill.Description = model.Description.Trim();
        skill.Pourcentage = model.Pourcentage;
        skill.ExamId = model.ExamId;
        skill.LastModifiedOnUtc = DateTime.UtcNow;
        ExamAdminTranslationMapper.SetSkill(skill, "fr", model.Name, model.Description);
        ExamAdminTranslationMapper.SetSkill(skill, "en", model.EnglishName, model.EnglishDescription);

        if (model.Id == Guid.Empty)
            _db.Set<Skill>().Add(skill);

        await _db.SaveChangesAsync();
        model.Id = skill.Id;
        return true;
    }

    public async Task<bool> DeleteSkillAsync(Guid id)
    {
        var hasQuestions = await _db.Set<Question>().AnyAsync(q => q.SkillId == id);
        if (hasQuestions)
            return false;

        var skill = await _db.Set<Skill>().FirstOrDefaultAsync(s => s.Id == id);
        if (skill == null)
            return false;
        _db.Set<Skill>().Remove(skill);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SkillSelectItemViewModel>> GetSkillOptionsAsync(Guid? examId = null)
    {
        var query = _db.Set<Skill>().AsNoTracking().AsQueryable();
        if (examId.HasValue && examId.Value != Guid.Empty)
            query = query.Where(s => s.ExamId == examId.Value);
        return await query.OrderBy(s => s.Name)
            .Select(s => new SkillSelectItemViewModel { Id = s.Id, ExamId = s.ExamId, Name = s.Name ?? string.Empty })
            .ToListAsync();
    }
}

public sealed class QuestionAdminService : IQuestionAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IExamAdminService _exams;
    private readonly ISkillAdminService _skills;
    private readonly IStringLocalizer<SharedResource> _text;

    public QuestionAdminService(ApplicationDbContext db, IExamAdminService exams, ISkillAdminService skills, IStringLocalizer<SharedResource> text)
    {
        _db = db;
        _exams = exams;
        _skills = skills;
        _text = text;
    }

    public async Task<QuestionIndexViewModel> GetQuestionsAsync(string? search, Guid? examId, Guid? skillId, QuestionType? type, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        search = (search ?? string.Empty).Trim();
        pageSize = AdminPagination.NormalizePageSize(pageSize);
        page = AdminPagination.NormalizePage(page);
        var query = _db.Set<Question>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.Description != null && q.Description.Contains(search));
        if (examId.HasValue && examId.Value != Guid.Empty)
            query = query.Where(q => q.ExamId == examId.Value);
        if (skillId.HasValue && skillId.Value != Guid.Empty)
            query = query.Where(q => q.SkillId == skillId.Value);
        if (type.HasValue)
            query = query.Where(q => q.QuestionType == type.Value);
        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = AdminPagination.ClampPage(page, totalPages);

        return new QuestionIndexViewModel
        {
            Search = search,
            ExamId = examId,
            SkillId = skillId,
            QuestionType = type,
            Status = status,
            Pagination = new AdminPaginationViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Controller = "Question",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search,
                    ["examId"] = examId?.ToString() ?? string.Empty,
                    ["skillId"] = skillId?.ToString() ?? string.Empty,
                    ["questionType"] = type?.ToString() ?? string.Empty,
                    ["status"] = status?.ToString() ?? string.Empty,
                    ["pageSize"] = pageSize.ToString()
                }
            },
            Exams = await _exams.GetExamOptionsAsync(),
            Skills = await _skills.GetSkillOptionsAsync(examId.HasValue && examId.Value != Guid.Empty ? examId : null),
            Questions = await query.OrderByDescending(q => q.CreatedOnUtc).ThenBy(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuestionRowViewModel
            {
                Id = q.Id,
                Description = q.Description ?? string.Empty,
                ExamName = q.Exam == null ? string.Empty : q.Exam.Name ?? string.Empty,
                SkillName = q.Skill == null ? "General" : q.Skill.Name ?? "General",
                QuestionType = q.QuestionType,
                Status = q.Status,
                ChoicesCount = q.Choices == null ? 0 : q.Choices.Count
            }).ToListAsync()
        };
    }

    public async Task<QuestionEditViewModel> NewQuestionAsync(Guid? examId) =>
        await PopulateOptionsAsync(new QuestionEditViewModel
        {
            ExamId = examId ?? Guid.Empty,
            Choices = new List<ChoiceEditViewModel>
            {
                new(),
                new()
            },
            DragDropPairs = new List<DragDropPairViewModel> { new(), new() },
            GroupedChoices = new List<GroupedChoiceViewModel> { new(), new() }
        });

    public async Task<QuestionEditViewModel?> GetQuestionAsync(Guid id)
    {
        var model = await _db.Set<Question>().AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuestionEditViewModel
            {
                Id = q.Id,
                Description = q.Description ?? string.Empty,
                EnglishDescription = q.Translations!.Where(t => t.Culture == "en").Select(t => t.Description).FirstOrDefault() ?? string.Empty,
                Explication = q.Explication,
                EnglishExplication = q.Translations!.Where(t => t.Culture == "en").Select(t => t.Explanation).FirstOrDefault(),
                FillInBlank = q.FillInBlank,
                EnglishFillInBlank = q.Translations!.Where(t => t.Culture == "en").Select(t => t.FillInBlank).FirstOrDefault(),
                QuestionType = q.QuestionType,
                Status = q.Status,
                ExamId = q.ExamId ?? Guid.Empty,
                SkillId = q.SkillId,
                Choices = q.Choices == null ? new List<ChoiceEditViewModel>() : q.Choices.Select(c => new ChoiceEditViewModel
                {
                    Id = c.Id,
                    ChoiceText = c.ChoiceText ?? string.Empty,
                    EnglishChoiceText = c.Translations!.Where(t => t.Culture == "en").Select(t => t.ChoiceText).FirstOrDefault() ?? string.Empty,
                    IsCorrect = c.IsCorrect,
                    GroupBy = c.GroupBy,
                    EnglishGroupBy = c.Translations!.Where(t => t.Culture == "en").Select(t => t.GroupBy).FirstOrDefault()
                }).ToList()
            }).FirstOrDefaultAsync();

        if (model == null)
            return null;

        MapChoicesToQuestionTypeViewModel(model);
        return await PopulateOptionsAsync(model);
    }

    public async Task<QuestionDetailsViewModel?> GetQuestionDetailsAsync(Guid id) =>
        await _db.Set<Question>().AsNoTracking()
            .Where(question => question.Id == id)
            .Select(question => new QuestionDetailsViewModel
            {
                Id = question.Id,
                Description = question.Description ?? string.Empty,
                EnglishDescription = question.Translations!
                    .Where(translation => translation.Culture == "en")
                    .Select(translation => translation.Description)
                    .FirstOrDefault() ?? string.Empty,
                Explication = question.Explication,
                EnglishExplication = question.Translations!
                    .Where(translation => translation.Culture == "en")
                    .Select(translation => translation.Explanation)
                    .FirstOrDefault(),
                QuestionType = question.QuestionType,
                Status = question.Status,
                ExamName = question.Exam == null ? string.Empty : question.Exam.Name ?? string.Empty,
                SkillName = question.Skill == null ? string.Empty : question.Skill.Name ?? string.Empty,
                Choices = question.Choices!.Select(choice => new QuestionChoiceDetailsViewModel
                {
                    ChoiceText = choice.ChoiceText ?? string.Empty,
                    EnglishChoiceText = choice.Translations!
                        .Where(translation => translation.Culture == "en")
                        .Select(translation => translation.ChoiceText)
                        .FirstOrDefault() ?? string.Empty,
                    IsCorrect = choice.IsCorrect
                }).ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<bool> SaveQuestionAsync(QuestionEditViewModel model, ModelStateDictionaryProxy modelState)
    {
        NormalizeQuestionByType(model);
        ValidateQuestionByType(model, modelState);

        var examExists = model.ExamId != Guid.Empty && await _db.Set<Exam>().AnyAsync(e => e.Id == model.ExamId);
        if (!examExists)
            modelState.Add(nameof(model.ExamId), _text["Admin.ExistingExamRequired"]);

        if (model.SkillId.HasValue && model.SkillId.Value != Guid.Empty &&
            !await _db.Set<Skill>().AnyAsync(s => s.Id == model.SkillId.Value && s.ExamId == model.ExamId))
        {
            modelState.Add(nameof(model.SkillId), _text["Question.SkillMustBelongToExam"]);
        }

        if (modelState.HasErrors)
            return false;

        var question = model.Id == Guid.Empty
            ? new Question { CreatedOnUtc = DateTime.UtcNow, Translations = new List<QuestionTranslation>() }
            : await _db.Set<Question>().Include(q => q.Translations)
                .Include(q => q.Choices!).ThenInclude(choice => choice.Translations)
                .FirstOrDefaultAsync(q => q.Id == model.Id);
        if (question == null)
            return false;

        var previousExamId = question.ExamId;

        question.Description = model.Description.Trim();
        question.Explication = model.Explication?.Trim();
        question.FillInBlank = model.FillInBlank?.Trim();
        question.QuestionType = model.QuestionType;
        question.Status = model.Status;
        question.ExamId = model.ExamId;
        question.SkillId = model.SkillId == Guid.Empty ? null : model.SkillId;
        question.LastModifiedOnUtc = DateTime.UtcNow;
        ExamAdminTranslationMapper.SetQuestion(question, "fr", model.Description, model.Explication, model.FillInBlank);
        ExamAdminTranslationMapper.SetQuestion(question, "en", model.EnglishDescription, model.EnglishExplication, model.EnglishFillInBlank);

        if (model.Id == Guid.Empty)
        {
            question.Choices = model.Choices.Select(choiceModel =>
            {
                var choice = ToChoice(choiceModel);
                ExamAdminTranslationMapper.SetChoice(choice, "fr", choiceModel.ChoiceText, choiceModel.GroupBy);
                ExamAdminTranslationMapper.SetChoice(choice, "en", choiceModel.EnglishChoiceText, choiceModel.EnglishGroupBy);
                return choice;
            }).ToList();
            _db.Set<Question>().Add(question);
        }
        else
        {
            var incomingIds = model.Choices.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToHashSet();
            foreach (var existing in question.Choices?.Where(c => !incomingIds.Contains(c.Id)).ToList() ?? new List<Choice>())
                _db.Set<Choice>().Remove(existing);

            foreach (var choiceModel in model.Choices)
            {
                var choice = question.Choices?.FirstOrDefault(c => c.Id == choiceModel.Id);
                if (choice == null)
                {
                    choice = ToChoice(choiceModel);
                    choice.QuestionId = question.Id;
                    ExamAdminTranslationMapper.SetChoice(choice, "fr", choiceModel.ChoiceText, choiceModel.GroupBy);
                    ExamAdminTranslationMapper.SetChoice(choice, "en", choiceModel.EnglishChoiceText, choiceModel.EnglishGroupBy);
                    _db.Set<Choice>().Add(choice);
                }
                else
                {
                    choice.ChoiceText = choiceModel.ChoiceText.Trim();
                    choice.IsCorrect = choiceModel.IsCorrect;
                    choice.GroupBy = choiceModel.GroupBy?.Trim();
                    choice.LastModifiedOnUtc = DateTime.UtcNow;
                    ExamAdminTranslationMapper.SetChoice(choice, "fr", choiceModel.ChoiceText, choiceModel.GroupBy);
                    ExamAdminTranslationMapper.SetChoice(choice, "en", choiceModel.EnglishChoiceText, choiceModel.EnglishGroupBy);
                }
            }
        }

        await _db.SaveChangesAsync();
        model.Id = question.Id;

        foreach (var examId in new[] { previousExamId, model.ExamId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            var exam = await _db.Set<Exam>().FirstOrDefaultAsync(e => e.Id == examId);
            if (exam != null)
                exam.QuestionsCount = await _db.Set<Question>().CountAsync(q => q.ExamId == examId && q.Status == Status.Published);
        }

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteQuestionAsync(Guid id)
    {
        var question = await _db.Set<Question>().Include(q => q.Choices).FirstOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return false;

        var examId = question.ExamId;
        _db.Set<Question>().Remove(question);
        await _db.SaveChangesAsync();

        if (examId.HasValue)
        {
            var exam = await _db.Set<Exam>().FirstOrDefaultAsync(e => e.Id == examId.Value);
            if (exam != null)
            {
                exam.QuestionsCount = await _db.Set<Question>().CountAsync(q => q.ExamId == examId.Value && q.Status == Status.Published);
                await _db.SaveChangesAsync();
            }
        }

        return true;
    }

    private async Task<QuestionEditViewModel> PopulateOptionsAsync(QuestionEditViewModel model)
    {
        model.Exams = await _exams.GetExamOptionsAsync();
        model.Skills = await _skills.GetSkillOptionsAsync(model.ExamId == Guid.Empty ? null : model.ExamId);
        return model;
    }

    private static Choice ToChoice(ChoiceEditViewModel model) => new()
    {
        ChoiceText = model.ChoiceText.Trim(),
        IsCorrect = model.IsCorrect,
        GroupBy = model.GroupBy?.Trim(),
        CreatedOnUtc = DateTime.UtcNow
    };

    private static void MapChoicesToQuestionTypeViewModel(QuestionEditViewModel model)
    {
        var correctChoices = model.Choices.Where(c => c.IsCorrect).ToList();

        if (model.QuestionType is QuestionType.ShortAnswer or QuestionType.FillInBlank)
        {
            model.ExpectedAnswer = correctChoices.FirstOrDefault()?.ChoiceText;
            model.AcceptedAnswers = string.Join(Environment.NewLine, correctChoices.Skip(1).Select(c => c.ChoiceText));
            model.EnglishExpectedAnswer = correctChoices.FirstOrDefault()?.EnglishChoiceText;
            model.EnglishAcceptedAnswers = string.Join(Environment.NewLine, correctChoices.Skip(1).Select(c => c.EnglishChoiceText).Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        if (model.QuestionType == QuestionType.DragAndDrop)
        {
            model.DragDropPairs = model.Choices
                .Select(c => new DragDropPairViewModel { Id = c.Id, Source = c.ChoiceText, Target = c.GroupBy ?? string.Empty, EnglishSource = c.EnglishChoiceText, EnglishTarget = c.EnglishGroupBy ?? string.Empty })
                .ToList();
        }

        if (model.QuestionType == QuestionType.DragAndDrop2)
        {
            model.GroupedChoices = model.Choices
                .Select(c => new GroupedChoiceViewModel { Id = c.Id, Item = c.ChoiceText, Group = c.GroupBy ?? string.Empty, EnglishItem = c.EnglishChoiceText, EnglishGroup = c.EnglishGroupBy ?? string.Empty })
                .ToList();
        }

        if (model.Choices.Count < 2 && model.QuestionType is QuestionType.SingleChoice or QuestionType.MultiChoice)
            model.Choices.AddRange(Enumerable.Range(0, 2 - model.Choices.Count).Select(_ => new ChoiceEditViewModel()));

        if (model.DragDropPairs.Count < 2)
            model.DragDropPairs.AddRange(Enumerable.Range(0, 2 - model.DragDropPairs.Count).Select(_ => new DragDropPairViewModel()));

        if (model.GroupedChoices.Count < 2)
            model.GroupedChoices.AddRange(Enumerable.Range(0, 2 - model.GroupedChoices.Count).Select(_ => new GroupedChoiceViewModel()));
    }

    private static void NormalizeQuestionByType(QuestionEditViewModel model)
    {
        model.Description = model.Description.Trim();
        model.EnglishDescription = model.EnglishDescription?.Trim() ?? string.Empty;
        model.Explication = model.Explication?.Trim();
        model.EnglishExplication = model.EnglishExplication?.Trim();
        model.FillInBlank = model.FillInBlank?.Trim();
        model.EnglishFillInBlank = model.EnglishFillInBlank?.Trim();

        switch (model.QuestionType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.MultiChoice:
                model.Choices = CleanChoices(model.Choices);
                break;
            case QuestionType.TrueFalse:
                var trueIsCorrect = model.Choices.Any(c => string.Equals(c.ChoiceText, "True", StringComparison.OrdinalIgnoreCase) && c.IsCorrect);
                var falseIsCorrect = model.Choices.Any(c => string.Equals(c.ChoiceText, "False", StringComparison.OrdinalIgnoreCase) && c.IsCorrect);
                model.Choices = new List<ChoiceEditViewModel>
                {
                    new() { ChoiceText = "Vrai", EnglishChoiceText = "True", IsCorrect = trueIsCorrect || !falseIsCorrect },
                    new() { ChoiceText = "Faux", EnglishChoiceText = "False", IsCorrect = falseIsCorrect }
                };
                model.FillInBlank = null;
                model.EnglishFillInBlank = null;
                break;
            case QuestionType.ShortAnswer:
                model.FillInBlank = null;
                model.EnglishFillInBlank = null;
                model.Choices = TextAnswersToChoices(model.ExpectedAnswer, model.AcceptedAnswers, model.EnglishExpectedAnswer, model.EnglishAcceptedAnswers);
                break;
            case QuestionType.LongAnswer:
                model.FillInBlank = null;
                model.EnglishFillInBlank = null;
                model.Choices = new List<ChoiceEditViewModel>();
                break;
            case QuestionType.FillInBlank:
                model.Choices = TextAnswersToChoices(model.ExpectedAnswer, model.AcceptedAnswers, model.EnglishExpectedAnswer, model.EnglishAcceptedAnswers);
                break;
            case QuestionType.DragAndDrop:
                model.FillInBlank = null;
                model.Choices = model.DragDropPairs
                    .Where(p => !string.IsNullOrWhiteSpace(p.Source) || !string.IsNullOrWhiteSpace(p.Target))
                    .Select(p => new ChoiceEditViewModel { Id = p.Id, ChoiceText = p.Source.Trim(), GroupBy = p.Target.Trim(), EnglishChoiceText = p.EnglishSource?.Trim() ?? string.Empty, EnglishGroupBy = p.EnglishTarget?.Trim(), IsCorrect = true })
                    .ToList();
                break;
            case QuestionType.DragAndDrop2:
                model.FillInBlank = null;
                model.Choices = model.GroupedChoices
                    .Where(g => !string.IsNullOrWhiteSpace(g.Item) || !string.IsNullOrWhiteSpace(g.Group))
                    .Select(g => new ChoiceEditViewModel { Id = g.Id, ChoiceText = g.Item.Trim(), GroupBy = g.Group.Trim(), EnglishChoiceText = g.EnglishItem?.Trim() ?? string.Empty, EnglishGroupBy = g.EnglishGroup?.Trim(), IsCorrect = true })
                    .ToList();
                break;
        }
    }

    private static List<ChoiceEditViewModel> CleanChoices(IEnumerable<ChoiceEditViewModel> choices) =>
        choices.Where(c => !string.IsNullOrWhiteSpace(c.ChoiceText))
            .Select(c => new ChoiceEditViewModel
            {
                Id = c.Id,
                ChoiceText = c.ChoiceText.Trim(),
                EnglishChoiceText = c.EnglishChoiceText?.Trim() ?? string.Empty,
                GroupBy = c.GroupBy?.Trim(),
                EnglishGroupBy = c.EnglishGroupBy?.Trim(),
                IsCorrect = c.IsCorrect
            })
            .ToList();

    private static List<ChoiceEditViewModel> TextAnswersToChoices(string? expectedAnswer, string? acceptedAnswers, string? englishExpectedAnswer, string? englishAcceptedAnswers)
    {
        var answers = new List<string>();
        if (!string.IsNullOrWhiteSpace(expectedAnswer))
            answers.Add(expectedAnswer.Trim());

        answers.AddRange((acceptedAnswers ?? string.Empty)
            .Split(new[] { "\r\n", "\n", ";", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var englishAnswers = new List<string>();
        if (!string.IsNullOrWhiteSpace(englishExpectedAnswer))
            englishAnswers.Add(englishExpectedAnswer.Trim());
        englishAnswers.AddRange((englishAcceptedAnswers ?? string.Empty)
            .Split(new[] { "\r\n", "\n", ";", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return answers
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((a, index) => new ChoiceEditViewModel
            {
                ChoiceText = a.Trim(),
                EnglishChoiceText = index < englishAnswers.Count ? englishAnswers[index].Trim() : string.Empty,
                IsCorrect = true
            })
            .ToList();
    }

    private void ValidateQuestionByType(QuestionEditViewModel model, ModelStateDictionaryProxy modelState)
    {
        switch (model.QuestionType)
        {
            case QuestionType.SingleChoice:
                if (model.Choices.Count < 2)
                    modelState.Add(nameof(model.Choices), _text["Question.SingleChoiceMinOptions"]);
                if (model.Choices.Count(c => c.IsCorrect) != 1)
                    modelState.Add(nameof(model.Choices), _text["Question.SelectExactlyOneCorrect"]);
                break;
            case QuestionType.MultiChoice:
                if (model.Choices.Count < 2)
                    modelState.Add(nameof(model.Choices), _text["Question.MultiChoiceMinOptions"]);
                if (!model.Choices.Any(c => c.IsCorrect))
                    modelState.Add(nameof(model.Choices), _text["Question.SelectOneOrMoreCorrect"]);
                break;
            case QuestionType.TrueFalse:
                if (model.Choices.Count != 2)
                    modelState.Add(nameof(model.Choices), _text["Question.TrueFalseExactlyTwo"]);
                if (model.Choices.Count(c => c.IsCorrect) != 1)
                    modelState.Add(nameof(model.Choices), _text["Question.SelectTrueOrFalse"]);
                break;
            case QuestionType.ShortAnswer:
                if (string.IsNullOrWhiteSpace(model.ExpectedAnswer))
                    modelState.Add(nameof(model.ExpectedAnswer), _text["Question.ExpectedAnswerRequired"]);
                break;
            case QuestionType.LongAnswer:
                if (string.IsNullOrWhiteSpace(model.Explication))
                    modelState.Add(nameof(model.Explication), _text["Question.GradingCriteriaRequired"]);
                break;
            case QuestionType.FillInBlank:
                if (string.IsNullOrWhiteSpace(model.FillInBlank))
                    modelState.Add(nameof(model.FillInBlank), _text["Question.BlankTextRequired"]);
                if (string.IsNullOrWhiteSpace(model.ExpectedAnswer))
                    modelState.Add(nameof(model.ExpectedAnswer), _text["Question.CorrectAnswerRequired"]);
                break;
            case QuestionType.DragAndDrop:
                if (model.Choices.Count < 2)
                    modelState.Add(nameof(model.DragDropPairs), _text["Question.AddTwoMatchingPairs"]);
                if (model.Choices.Any(c => string.IsNullOrWhiteSpace(c.ChoiceText) || string.IsNullOrWhiteSpace(c.GroupBy)))
                    modelState.Add(nameof(model.DragDropPairs), _text["Question.MatchingPairIncomplete"]);
                break;
            case QuestionType.DragAndDrop2:
                if (model.Choices.Count < 2)
                    modelState.Add(nameof(model.GroupedChoices), _text["Question.AddTwoGroupedItems"]);
                if (model.Choices.Any(c => string.IsNullOrWhiteSpace(c.ChoiceText) || string.IsNullOrWhiteSpace(c.GroupBy)))
                    modelState.Add(nameof(model.GroupedChoices), _text["Question.GroupedItemIncomplete"]);
                if (model.Choices.Select(c => c.GroupBy?.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
                    modelState.Add(nameof(model.GroupedChoices), _text["Question.AddTwoDistinctGroups"]);
                break;
        }
    }

    private void ValidateChoices(QuestionEditViewModel model, ModelStateDictionaryProxy modelState)
    {
        if (model.QuestionType is QuestionType.ShortAnswer or QuestionType.LongAnswer)
            return;

        if (!model.Choices.Any())
            modelState.Add(nameof(model.Choices), _text["Question.AddAtLeastOneAnswer"]);

        var correctCount = model.Choices.Count(c => c.IsCorrect);
        if (correctCount == 0)
            modelState.Add(nameof(model.Choices), _text["Question.MarkCorrectAnswer"]);

        if ((model.QuestionType == QuestionType.SingleChoice || model.QuestionType == QuestionType.TrueFalse) && correctCount > 1)
            modelState.Add(nameof(model.Choices), _text["Question.OnlyOneCorrectAnswer"]);
    }
}
