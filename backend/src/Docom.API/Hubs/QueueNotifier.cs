using Docom.Domain.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Docom.API.Hubs;

public class QueueNotifier : IQueueNotifier
{
    private readonly IHubContext<QueueHub> _hub;

    public QueueNotifier(IHubContext<QueueHub> hub) => _hub = hub;

    public Task NotifyQueueAdvancedAsync(string doctorSlug, int currentToken, int waitingCount)
        => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
               .SendAsync("QueueAdvanced", new
               {
                   currentToken,
                   waitingCount,
                   estimatedWaitMinutes = waitingCount * 5
               });

    public Task NotifyTokenCreatedAsync(string doctorSlug, int tokenNumber, int queuePosition)
        => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
               .SendAsync("TokenCreated", new { tokenNumber, queuePosition });

    public Task NotifyTokenSkippedAsync(string doctorSlug, int skippedToken, int currentToken)
        => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
               .SendAsync("TokenSkipped", new { skippedToken, currentToken });

    public Task NotifySessionChangedAsync(string doctorSlug, string status)
        => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
               .SendAsync("SessionChanged", new { status });
}
