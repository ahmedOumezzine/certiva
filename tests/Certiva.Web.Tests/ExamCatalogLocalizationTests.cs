using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamCatalogLocalizationTests
{
    [Fact]
    public async Task CatalogAndDetailsUseRequestedTranslationAndFallBackToFrench()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Réseaux",
            Description = "Compétence réseau",
            Pourcentage = 100,
            CreatedOnUtc = DateTime.UtcNow,
            Translations =
            [
                new SkillTranslation
                {
                    Id = Guid.NewGuid(),
                    Culture = "en",
                    Name = "Networking",
                    Description = "Networking skills",
                    CreatedOnUtc = DateTime.UtcNow
                }
            ]
        };
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Réseaux essentiels",
            Description = "Examen de réseaux",
            Code = "NET",
            Slug = "reseaux-essentiels",
            Status = Status.Published,
            CreatedOnUtc = DateTime.UtcNow,
            Skills = [skill],
            Questions =
            [
                new Question
                {
                    Id = Guid.NewGuid(),
                    Description = "Décrivez le DNS.",
                    QuestionType = QuestionType.LongAnswer,
                    Status = Status.Published,
                    CreatedOnUtc = DateTime.UtcNow,
                    Skill = skill,
                    Translations =
                    [
                        new QuestionTranslation
                        {
                            Id = Guid.NewGuid(),
                            Culture = "en",
                            Description = "Describe DNS.",
                            CreatedOnUtc = DateTime.UtcNow
                        }
                    ]
                }
            ],
            Translations =
            [
                new ExamTranslation
                {
                    Id = Guid.NewGuid(),
                    Culture = "en",
                    Name = "Essential Networking",
                    Description = "A networking practice exam",
                    Slug = "essential-networking",
                    CreatedOnUtc = DateTime.UtcNow
                }
            ]
        };

        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        var catalog = new EfExamCatalogReader(db);

        var englishList = await catalog.GetCatalogAsync(null, null, 1, 9, "en");
        var searchResults = await catalog.GetCatalogAsync("essential", null, 1, 9, "en");
        var englishDetails = await catalog.GetDetailsAsync("essential-networking", "en");
        var frenchFallback = await catalog.GetDetailsAsync("reseaux-essentiels", "en");

        var card = Assert.Single(englishList.Exams);
        Assert.Single(searchResults.Exams);
        Assert.Equal("Essential Networking", card.Name);
        Assert.Equal("A networking practice exam", card.Description);
        Assert.Equal("essential-networking", card.Slug);
        Assert.Contains("Networking", card.Skills);
        Assert.NotNull(englishDetails);
        Assert.Equal("Describe DNS.", Assert.Single(englishDetails!.Questions).Text);
        Assert.Equal("Networking", Assert.Single(englishDetails.Skills).Name);
        Assert.True(englishDetails.HasCompleteTranslation);
        Assert.True(englishDetails.HasCompleteEnglishTranslation);
        Assert.False(englishDetails.HasEnglishFallback);
        Assert.Equal("reseaux-essentiels", englishDetails.FrenchSlug);
        Assert.Equal("essential-networking", englishDetails.EnglishSlug);
        Assert.NotNull(frenchFallback);
        Assert.Equal("Essential Networking", frenchFallback!.Name);
        Assert.Equal("reseaux-essentiels", frenchFallback.SourceSlug);

        var untranslated = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Examen sans traduction",
            Description = "Description en français",
            Code = "FR",
            Slug = "examen-sans-traduction",
            Status = Status.Published,
            CreatedOnUtc = DateTime.UtcNow,
            Skills = [],
            Questions = []
        };
        db.Exams.Add(untranslated);
        await db.SaveChangesAsync();

        var englishFallback = await catalog.GetDetailsAsync(untranslated.Slug!, "en");

        Assert.NotNull(englishFallback);
        Assert.True(englishFallback!.HasEnglishFallback);
        Assert.False(englishFallback.HasCompleteTranslation);
        Assert.False(englishFallback.HasCompleteEnglishTranslation);
        Assert.Equal("Examen sans traduction", englishFallback.Name);
    }
}
