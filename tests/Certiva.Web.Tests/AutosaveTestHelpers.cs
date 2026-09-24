using Certiva.Application.Attempts;
using Certiva.Application.Security;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Services.Exams;

namespace Certiva.Web.Tests;

internal static class AutosaveTestHelpers
{
    public static async Task<SaveAnswerResult> SaveAsync(ApplicationDbContext db, SaveExamAnswerRequest request, string? userId, string? token)
    {
        var result = await new ExamAttemptAutosaveService(new EfExamAttemptAnswerStore(db)).SaveAnswerAsync(new SaveAnswerRequest(request.AttemptId, request.AttemptQuestionId, request.SelectedChoiceIds, request.TextAnswer), userId, token);
        return new((SaveAnswerStatus)result.Status, result.SavedAtUtc);
    }
}
