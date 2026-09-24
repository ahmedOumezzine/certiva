namespace Certiva.Application.Attempts;

public interface IExamAttemptAutosaveService
{
    Task<SaveAnswerResultDto> SaveAnswerAsync(SaveAnswerRequest request, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}

public interface IExamAttemptAnswerStore
{
    Task<SaveAnswerResultDto> SaveAnswerAsync(SaveAnswerRequest request, string? userId, string? accessToken, CancellationToken cancellationToken = default);
}
