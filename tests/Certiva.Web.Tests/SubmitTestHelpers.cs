using Certiva.Application.Attempts;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Models.Exams;
using Certiva.Application.Security;

namespace Certiva.Web.Tests;

internal static class SubmitTestHelpers
{
    public static async Task<ExamResultViewModel?> SubmitAsync(ApplicationDbContext db, Guid attemptId, string? userId, string? token)
    {
        var dto = await new ExamAttemptSubmitService(new EfExamAttemptSubmitStore(db, new GuestAttemptTokenService())).SubmitAsync(attemptId, userId, token);
        return dto?.ToViewModel();
    }
}
