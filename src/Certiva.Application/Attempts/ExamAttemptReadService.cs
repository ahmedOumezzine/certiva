namespace Certiva.Application.Attempts;

public sealed class ExamAttemptReadService : IExamAttemptReadService
{
    private readonly IExamAttemptReader _reader;
    public ExamAttemptReadService(IExamAttemptReader reader) => _reader = reader;
    public Task<ResumeAttemptResultDto> ResumeAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default) => _reader.ResumeAsync(attemptId, userId, accessToken, cancellationToken);
    public Task<AttemptResultDto?> GetResultAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default) => _reader.GetResultAsync(attemptId, userId, accessToken, cancellationToken);
}

public interface IExamAttemptReadService
{
    Task<ResumeAttemptResultDto> ResumeAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
    Task<AttemptResultDto?> GetResultAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}
