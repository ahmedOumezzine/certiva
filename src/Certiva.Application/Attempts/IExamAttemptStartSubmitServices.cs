namespace Certiva.Application.Attempts;

public interface IExamAttemptStartService
{
    Task<StartedAttemptDto?> StartAsync(StartAttemptRequest request, CancellationToken cancellationToken = default);
}

public interface IExamAttemptSubmitService
{
    Task<AttemptResultDto?> SubmitAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}

public interface IExamAttemptStartStore
{
    Task<StartedAttemptDto?> StartAsync(StartAttemptRequest request, CancellationToken cancellationToken = default);
}

public interface IExamAttemptSubmitStore
{
    Task<AttemptResultDto?> SubmitAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}
