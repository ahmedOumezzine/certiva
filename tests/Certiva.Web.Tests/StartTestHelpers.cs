using Certiva.Application.Attempts;
using Certiva.Application.Security;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;

namespace Certiva.Web.Tests;

internal static class StartTestHelpers
{
    public static Task<StartedAttemptDto?> StartAsync(ApplicationDbContext db, string slug, Guid? skillId = null, bool random = false, string email = "candidate@example.com", string culture = "fr") =>
        new ExamAttemptStartService(new EfExamAttemptStartStore(db, new GuestAttemptTokenService())).StartAsync(new StartAttemptRequest(slug, skillId, random, email, culture));
}
