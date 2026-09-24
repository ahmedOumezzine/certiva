namespace Certiva.Application.Attempts;

public sealed class ExamAttemptStartService : IExamAttemptStartService
{
    private readonly IExamAttemptStartStore _store;
    public ExamAttemptStartService(IExamAttemptStartStore store) => _store = store;
    public Task<StartedAttemptDto?> StartAsync(StartAttemptRequest request, CancellationToken cancellationToken = default) => _store.StartAsync(request, cancellationToken);
}

public sealed class ExamAttemptSubmitService : IExamAttemptSubmitService
{
    private readonly IExamAttemptSubmitStore _store;
    public ExamAttemptSubmitService(IExamAttemptSubmitStore store) => _store = store;
    public Task<AttemptResultDto?> SubmitAsync(Guid attemptId, string? userId, string? accessToken, CancellationToken cancellationToken = default) => _store.SubmitAsync(attemptId, userId, accessToken, cancellationToken);
}
