using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Seed;
using Certiva.Services.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamDemoSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesFiveValidPublishedExamsAndIsIdempotent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"exam-demo-seed-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);

        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance);
        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance);

        var exams = await db.Exams
            .Include(x => x.Skills)
            .Include(x => x.Questions!)
                .ThenInclude(x => x.Choices)
            .ToListAsync();

        Assert.Equal(5, exams.Count);
        Assert.Equal(100, exams.Sum(x => x.Questions!.Count));

        foreach (var exam in exams)
        {
            Assert.Equal(4, exam.Skills!.Count);
            Assert.Equal(100, exam.Skills.Sum(x => x.Pourcentage));
            Assert.Equal(20, exam.Questions!.Count);
            Assert.All(exam.Questions, question =>
            {
                Assert.Equal(exam.Id, question.ExamId);
                Assert.NotNull(question.SkillId);
                Assert.Contains(exam.Skills, skill => skill.Id == question.SkillId && skill.ExamId == exam.Id);
                Assert.Empty(ExamPublicationValidator.Validate(exam, exam.Name, exam.Description,
                    exam.Code, exam.Slug, exam.DurationMinutes, exam.PassingPercentage));
            });
        }

        Assert.Empty(await db.ExamAttempts.ToListAsync());
        Assert.Empty(await db.ExamAttemptQuestions.ToListAsync());
        Assert.Empty(await db.ExamAttemptChoices.ToListAsync());
        Assert.Empty(await db.ExamAnswers.ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotRewriteExistingTranslations()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"exam-demo-seed-preserve-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);

        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance);

        var existingFrenchChoice = await db.ChoiceTranslations
            .Include(translation => translation.Choice)
            .FirstAsync(translation => translation.Culture == "fr"
                && translation.Choice!.ChoiceText == "True");
        existingFrenchChoice.ChoiceText = "Texte personnalisé";
        await db.SaveChangesAsync();

        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance);

        Assert.Equal("Texte personnalisé", existingFrenchChoice.ChoiceText);
    }

    [Fact]
    public async Task SeedAsync_ResetRemovesOnlyDemoExamsBeforeRecreatingThem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"exam-demo-reset-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance);
        db.Exams.Add(new Certiva.Domain.Exams.Exam
        {
            Id = Guid.NewGuid(), Name = "Real exam", Description = "Keep", Code = "REAL",
            Slug = "real-exam", Status = Certiva.Domain.Enums.Status.Published
        });
        await db.SaveChangesAsync();

        await ExamDemoSeeder.SeedAsync(db, NullLogger.Instance, resetDemoData: true);

        Assert.Equal(6, await db.Exams.CountAsync());
        Assert.Equal(1, await db.Exams.CountAsync(exam => exam.Code == "REAL"));
        Assert.Equal(5, await db.Exams.CountAsync(exam => exam.Code != "REAL"));
    }
}
