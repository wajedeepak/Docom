using Microsoft.AspNetCore.SignalR;

namespace Docom.API.Hubs;

/// <summary>
/// Patients join the group named after the doctor's slug.
/// All queue events are broadcast to that group.
/// </summary>
public class QueueHub : Hub
{
    public async Task JoinDoctorQueue(string doctorSlug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, doctorSlug.ToLowerInvariant());
    }

    public async Task LeaveDoctorQueue(string doctorSlug)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, doctorSlug.ToLowerInvariant());
    }
}
