using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Areas.Admin.Services;
using Certiva.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class AdminDashboardServiceTests
{
    [Fact]
    public async Task DashboardReturnsCountsRecentExamsAndQuestionCountsBySkill()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var olderExam = CreateExam("Older exam", Status.Draft, DateTime.UtcNow.AddDays(-1));
        var recentExam = CreateExam("Recent exam", Status.Published, DateTime.UtcNow);
        var databasesSkill = CreateSkill("Databases", recentExam.Id);
        var securitySkill = CreateSkill("Security");
        db.Set<Exam>().AddRange(olderExam, recentExam);
        db.Set<Skill>().AddRange(databasesSkill, securitySkill);
        db.Set<Question>().AddRange(
            CreateQuestion(olderExam.Id, databasesSkill.Id),
            CreateQuestion(recentExam.Id, databasesSkill.Id),
            CreateQuestion(recentExam.Id, securitySkill.Id));
        await db.SaveChangesAsync();

        var viewModel = await new ExamAdminService(db, null!).GetDashboardAsync();

        Assert.Equal(2, viewModel.ExamsCount);
        Assert.Equal(1, viewModel.PublishedCount);
        Assert.Equal(1, viewModel.DraftCount);
        Assert.Equal(0, viewModel.ArchivedCount);
        Assert.Equal(3, viewModel.QuestionsCount);
        Assert.Equal(2, viewModel.SkillsCount);
        Assert.Equal("Recent exam", viewModel.RecentExams[0].Name);
        Assert.Equal(2, viewModel.RecentExams[0].QuestionsCount);
        Assert.Equal(1, viewModel.RecentExams[0].SkillsCount);
        Assert.Equal("Databases", viewModel.QuestionsBySkill[0].SkillName);
        Assert.Equal(2, viewModel.QuestionsBySkill[0].QuestionsCount);
    }

    private static Exam CreateExam(string name, Status status, DateTime createdOnUtc) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "Dashboard test",
        Code = name[..2].ToUpperInvariant(),
        Slug = name.Replace(' ', '-').ToLowerInvariant(),
        Status = status,
        CreatedOnUtc = createdOnUtc,
        QuestionsCount = 2
    };

    private static Skill CreateSkill(string name, Guid? examId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "Dashboard test",
        Pourcentage = 50,
        ExamId = examId,
        CreatedOnUtc = DateTime.UtcNow
    };

    private static Question CreateQuestion(Guid examId, Guid skillId) => new()
    {
        Id = Guid.NewGuid(),
        ExamId = examId,
        SkillId = skillId,
        Description = "Dashboard test question",
        QuestionType = QuestionType.SingleChoice,
        Status = Status.Published,
        CreatedOnUtc = DateTime.UtcNow
    };
}
