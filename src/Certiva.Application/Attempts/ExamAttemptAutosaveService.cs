namespace Certiva.Application.Attempts;

public sealed class ExamAttemptAutosaveService : IExamAttemptAutosaveService
{
    private readonly IExamAttemptAnswerStore _store;
    public ExamAttemptAutosaveService(IExamAttemptAnswerStore store) => _store = store;
    public Task<SaveAnswerResultDto> SaveAnswerAsync(SaveAnswerRequest request, string? userId, string? accessToken, CancellationToken cancellationToken = default) => _store.SaveAnswerAsync(request, userId, accessToken, cancellationToken);
}
