using Microsoft.AspNetCore.SignalR;

namespace MistoPodii.Realtime;

public sealed class EventsHub : Hub
{
    public string Echo(string value)
    {
        if (value.Length > 256) throw new HubException("Повідомлення завелике.");
        return value;
    }
}
