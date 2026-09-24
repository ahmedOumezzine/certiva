using Certiva.Application.Attempts;
using Certiva.Application.Security;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Models.Exams;
using Certiva.Services.Exams;

namespace Certiva.Web.Tests;

internal static class ReadTestHelpers
{
    public static async Task<ResumedExamAttempt> ResumeAsync(ApplicationDbContext db, Guid attemptId, string? userId, string? token)
    {
        var result = await new ExamAttemptReadService(new EfExamAttemptReader(db)).ResumeAsync(attemptId, userId, token);
        return new((ResumeAttemptStatus)result.Status, result.Session?.ToViewModel());
    }

    public static async Task<ExamResultViewModel?> GetResultAsync(ApplicationDbContext db, Guid attemptId, string? userId, string? token)
    {
        var result = await new ExamAttemptReadService(new EfExamAttemptReader(db)).GetResultAsync(attemptId, userId, token);
        return result?.ToViewModel();
    }
}
