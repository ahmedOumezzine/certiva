namespace Certiva.Application.Attempts;

public interface IExamAttemptReader
{
    Task<ResumeAttemptResultDto> ResumeAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
    Task<AttemptResultDto?> GetResultAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}
