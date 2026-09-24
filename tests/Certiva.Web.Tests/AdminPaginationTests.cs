using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Areas.Admin.Services;
using Certiva.Areas.Admin.ViewModels;
using Certiva.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class AdminPaginationTests
{
    [Fact]
    public async Task Exams_pagination_applies_filters_before_count_and_page()
    {
        await using var db = CreateContext();
        var exams = Enumerable.Range(1, 60)
            .Select(index => CreateExam(index, index <= 30 ? Status.Published : Status.Draft))
            .ToList();
        db.Set<Exam>().AddRange(exams);
        await db.SaveChangesAsync();

        var service = new ExamAdminService(db, null!);
        var page = await service.GetExamsAsync("Exam", Status.Published, page: 2, pageSize: 25);

        Assert.Equal(30, page.Pagination.TotalItems);
        Assert.Equal(2, page.Pagination.CurrentPage);
        Assert.Equal(25, page.Pagination.PageSize);
        Assert.Equal(2, page.Pagination.TotalPages);
        Assert.Equal(5, page.Exams.Count);
        Assert.All(page.Exams, exam => Assert.Equal(Status.Published, exam.Status));
    }

    [Fact]
    public async Task Skills_pagination_preserves_exam_filter_and_limits_page_size()
    {
        await using var db = CreateContext();
        var firstExam = CreateExam(1);
        var secondExam = CreateExam(2);
        db.Set<Exam>().AddRange(firstExam, secondExam);
        db.Set<Skill>().AddRange(
            Enumerable.Range(1, 30).Select(index => CreateSkill(index, firstExam.Id, firstExam))
                .Concat(Enumerable.Range(31, 5).Select(index => CreateSkill(index, secondExam.Id, secondExam))));
        await db.SaveChangesAsync();

        var exams = new ExamAdminService(db, null!);
        var service = new SkillAdminService(db, exams, null!);
        var page = await service.GetSkillsAsync(firstExam.Id, search: "Skill", page: 2, pageSize: 500);

        Assert.Equal(30, page.Pagination.TotalItems);
        Assert.Equal(2, page.Pagination.CurrentPage);
        Assert.Equal(25, page.Pagination.PageSize);
        Assert.Equal(5, page.Skills.Count);
        Assert.Equal("Skill", page.Pagination.RouteValues["search"]);
        Assert.All(page.Skills, skill => Assert.Equal($"Exam 01", skill.ExamName));
    }

    [Fact]
    public async Task Questions_pagination_applies_search_and_exam_filter_and_clamps_out_of_range_page()
    {
        await using var db = CreateContext();
        var firstExam = CreateExam(1);
        var secondExam = CreateExam(2);
        db.Set<Exam>().AddRange(firstExam, secondExam);
        db.Set<Skill>().AddRange(
            CreateSkill(1, firstExam.Id, firstExam),
            CreateSkill(2, secondExam.Id, secondExam));
        var questions = Enumerable.Range(1, 30)
            .Select(index => CreateQuestion(index, firstExam.Id, $"SQL topic {index:00}"))
            .Concat(Enumerable.Range(31, 4)
                .Select(index => CreateQuestion(index, secondExam.Id, $"SQL topic {index:00}")))
            .ToList();
        db.Set<Question>().AddRange(questions);
        await db.SaveChangesAsync();

        var exams = new ExamAdminService(db, null!);
        var skills = new SkillAdminService(db, exams, null!);
        var service = new QuestionAdminService(db, exams, skills, null!);
        var firstPage = await service.GetQuestionsAsync("SQL topic", firstExam.Id, null, null, null, page: 1, pageSize: 25);
        var page = await service.GetQuestionsAsync("SQL topic", firstExam.Id, null, null, null, page: 9, pageSize: 25);

        Assert.Equal(1, firstPage.Pagination.CurrentPage);
        Assert.Equal(25, firstPage.Questions.Count);
        Assert.Equal(30, page.Pagination.TotalItems);
        Assert.Equal(2, page.Pagination.CurrentPage);
        Assert.Equal(2, page.Pagination.TotalPages);
        Assert.Equal(5, page.Questions.Count);
        Assert.All(page.Questions, question => Assert.StartsWith("SQL topic", question.Description));
        Assert.Equal(2, page.Exams.Count);
        Assert.Single(page.Skills);
    }

    [Fact]
    public void Pagination_normalizes_invalid_pages_and_sizes()
    {
        Assert.Equal(1, AdminPagination.NormalizePage(0));
        Assert.Equal(25, AdminPagination.NormalizePageSize(0));
        Assert.Equal(25, AdminPagination.NormalizePageSize(1000));
        Assert.Equal(100, AdminPagination.NormalizePageSize(100));
        Assert.Equal(3, AdminPagination.ClampPage(9, 3));
        Assert.Equal(1, AdminPagination.ClampPage(9, 0));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Exam CreateExam(int index, Status status = Status.Published) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Exam {index:00}",
        Description = "Pagination test exam",
        Code = $"E{index:0000}",
        Slug = $"exam-{index:00}",
        Status = status,
        CreatedOnUtc = DateTime.UtcNow
    };

    private static Skill CreateSkill(int index, Guid examId, Exam exam) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Skill {index:00}",
        Description = "Pagination test skill",
        Pourcentage = 10,
        ExamId = examId,
        Exam = exam,
        CreatedOnUtc = DateTime.UtcNow
    };

    private static Question CreateQuestion(int index, Guid examId, string description) => new()
    {
        Id = Guid.NewGuid(),
        Description = description,
        QuestionType = QuestionType.SingleChoice,
        Status = Status.Published,
        ExamId = examId,
        CreatedOnUtc = DateTime.UtcNow.AddMinutes(-index)
    };
}
