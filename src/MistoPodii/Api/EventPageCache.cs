using Microsoft.Extensions.Caching.Memory;

namespace MistoPodii.Api;

public sealed record EventPageData(IReadOnlyList<EventResponse> Items, bool HasNext);

public sealed class EventPageCache(IMemoryCache memoryCache)
{
    private long _version;

    public async Task<(EventPageData Page, bool Hit)> GetOrLoadAsync(int skip, int limit, Func<Task<EventPageData>> load)
    {
        var key = $"event-page:{Interlocked.Read(ref _version)}:{skip}:{limit}";
        if (memoryCache.TryGetValue<EventPageData>(key, out var cached) && cached is not null)
            return (cached, true);

        var page = await load();
        memoryCache.Set(key, page, TimeSpan.FromSeconds(30));
        return (page, false);
    }

    public void Invalidate() => Interlocked.Increment(ref _version);
}
