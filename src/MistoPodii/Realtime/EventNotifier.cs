using Microsoft.AspNetCore.SignalR;
using MistoPodii.Models;

namespace MistoPodii.Realtime;

public sealed class EventNotifier(IHubContext<EventsHub> hub, ILogger<EventNotifier> logger)
{
    public async Task PublishAsync(Event item, string action)
    {
        try
        {
            await hub.Clients.All.SendAsync("EventChanged", new
            {
                id = item.Id,
                title = item.Title,
                status = item.Status.ToString(),
                action,
                sentAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not broadcast event {EventId} change", item.Id);
        }
    }
}
