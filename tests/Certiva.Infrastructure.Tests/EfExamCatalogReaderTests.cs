using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Infrastructure.Tests;

public sealed class EfExamCatalogReaderTests
{
    [Fact]
    public async Task CatalogReturnsOnlyPublishedExamsAndNormalizesPaging()
    {
        await using var db = CreateContext();
        var published = CreateExam("Published", "published");
        var draft = CreateExam("Draft", "draft");
        draft.Status = Status.Draft;
        db.Exams.AddRange(published, draft);
        await db.SaveChangesAsync();

        var result = await new EfExamCatalogReader(db).GetCatalogAsync(null, null, 0, 100, "de");

        Assert.Equal("fr", result.Culture);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Exams);
        Assert.Equal("Published", result.Exams[0].Name);
        Assert.Equal(24, result.PageSize);
    }

    [Fact]
    public async Task CatalogSupportsSearchSkillFilterAndEnglishFallback()
    {
        await using var db = CreateContext();
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Security", Description = "Security", Pourcentage = 50 };
        var exam = CreateExam("French name", "exam");
        exam.Skills = [skill];
        exam.Translations.Add(new ExamTranslation { Culture = "en", Name = "English name", Description = "English description", Slug = "english-exam" });
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        var result = await new EfExamCatalogReader(db).GetCatalogAsync("English", skill.Id, 1, 10, "en");

        Assert.Single(result.Exams);
        Assert.Equal("English name", result.Exams[0].Name);
        Assert.Equal("english-exam", result.Exams[0].Slug);
        Assert.True(result.Exams[0].HasEnglishFallback);
        Assert.Single(result.Skills);
    }

    [Fact]
    public async Task DetailsReturnNullForBlankOrUnknownSlug()
    {
        await using var db = CreateContext();
        var reader = new EfExamCatalogReader(db);

        Assert.Null(await reader.GetDetailsAsync(" "));
        Assert.Null(await reader.GetDetailsAsync("missing"));
    }

    [Fact]
    public async Task EnglishCatalogCompletenessDetectsMissingTranslation()
    {
        await using var db = CreateContext();
        db.Exams.Add(CreateExam("French", "exam"));
        await db.SaveChangesAsync();

        Assert.False(await new EfExamCatalogReader(db).HasCompleteEnglishCatalogAsync());
    }

    [Fact]
    public async Task DetailsUseEnglishTranslationsAndPublishedQuestionSummaries()
    {
        await using var db = CreateContext();
        var exam = CreateExam("Nom français", "examen");
        exam.Translations.Add(new ExamTranslation { Culture = "en", Name = "English exam", Description = "English description", Slug = "english-exam" });
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Sécurité", Description = "Description", Pourcentage = 60, ExamId = exam.Id };
        skill.Translations.Add(new SkillTranslation { Culture = "en", Name = "Security", Description = "Security description" });
        exam.Skills = [skill];
        exam.Questions =
        [
            new Question { Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Question française", Status = Status.Published, QuestionType = QuestionType.SingleChoice, SkillId = skill.Id,
                Translations = [new QuestionTranslation { Culture = "en", Description = "English question" }] },
            new Question { Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Draft", Status = Status.Draft, QuestionType = QuestionType.ShortAnswer },
        ];
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        var result = await new EfExamCatalogReader(db).GetDetailsAsync("english-exam", "en");

        Assert.NotNull(result);
        Assert.Equal("English exam", result!.Name);
        Assert.Equal("english-exam", result.Slug);
        Assert.Equal(1, result.QuestionsCount);
        Assert.Equal("Security", Assert.Single(result.Skills).Name);
        Assert.Equal("English question", Assert.Single(result.Questions).Text);
        Assert.Equal(QuestionType.SingleChoice, Assert.Single(result.QuestionTypes).Type);
    }

    [Fact]
    public async Task CompleteEnglishCatalogReturnsTrueWhenPublishedContentIsTranslated()
    {
        await using var db = CreateContext();
        var exam = CreateExam("French", "exam");
        exam.Translations.Add(new ExamTranslation { Culture = "en", Name = "English", Description = "Description", Slug = "exam-en" });
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Skill", Description = "Skill description", Pourcentage = 50, ExamId = exam.Id };
        skill.Translations.Add(new SkillTranslation { Culture = "en", Name = "Skill", Description = "Skill description" });
        exam.Skills = [skill];
        exam.Questions = [new Question { Id = Guid.NewGuid(), ExamId = exam.Id, Description = "Question", Status = Status.Published, QuestionType = QuestionType.ShortAnswer,
            Translations = [new QuestionTranslation { Culture = "en", Description = "Question" }] }];
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        Assert.True(await new EfExamCatalogReader(db).HasCompleteEnglishCatalogAsync());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Exam CreateExam(string name, string slug) => new()
    {
        Id = Guid.NewGuid(), Name = name, Description = "Description", Code = "CERT",
        Slug = slug, Status = Status.Published, QuestionsCount = 1, DurationMinutes = 30,
    };
}
