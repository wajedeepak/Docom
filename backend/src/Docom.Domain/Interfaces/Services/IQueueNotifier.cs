namespace Docom.Domain.Interfaces.Services;

public interface IQueueNotifier
{
    Task NotifyQueueAdvancedAsync(string doctorSlug, int currentToken, int waitingCount);
    Task NotifyTokenCreatedAsync(string doctorSlug, int tokenNumber, int queuePosition);
    Task NotifyTokenSkippedAsync(string doctorSlug, int skippedToken, int currentToken);
    Task NotifySessionChangedAsync(string doctorSlug, string status);
}
