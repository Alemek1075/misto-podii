using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;
using MistoPodii.Models;
using MistoPodii.Realtime;

namespace MistoPodii.Api;

[ApiController]
[Route("api/events")]
public sealed class EventsApiController(AppDbContext db, EventPageCache pageCache, EventNotifier notifier) : ControllerBase
{
    [HttpGet(Name = "ListEvents")]
    public async Task<ActionResult<PageResponse<EventResponse>>> List([FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        if (skip < 0 || limit is < 1 or > 100)
            return BadRequest(new { error = "skip має бути невід'ємним, limit має бути від 1 до 100." });

        var (page, hit) = await pageCache.GetOrLoadAsync(skip, limit, async () =>
        {
            var items = await db.Events.AsNoTracking().OrderBy(e => e.Id).Skip(skip).Take(limit + 1)
                .Select(e => new EventResponse(e.Id, e.Title, e.Description, e.Category, e.Status.ToString(), e.StartsAtUtc,
                    e.ImageUrl, e.OrganizerId, e.Organizer.Name, e.VenueId, e.Venue.Name)).ToListAsync();
            var hasNext = items.Count > limit;
            if (hasNext) items.RemoveAt(items.Count - 1);
            return new EventPageData(items.Select(WithUtcKind).ToList(), hasNext);
        });
        Response.Headers["X-Cache"] = hit ? "HIT" : "MISS";
        var nextLink = page.HasNext ? Url.Link("ListEvents", new { skip = skip + limit, limit }) : null;
        return Ok(new PageResponse<EventResponse>(page.Items, skip, limit, nextLink));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventResponse>> Get(int id)
    {
        var item = await db.Events.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new EventResponse(e.Id, e.Title, e.Description, e.Category, e.Status.ToString(), e.StartsAtUtc,
                e.ImageUrl, e.OrganizerId, e.Organizer.Name, e.VenueId, e.Venue.Name)).FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(WithUtcKind(item));
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create(EventInput input)
    {
        var relationError = await ValidateRelationsAsync(input);
        if (relationError is not null) return BadRequest(new { error = relationError });

        var item = new Event
        {
            Title = input.Title.Trim(), Description = input.Description.Trim(), Category = input.Category.Trim(),
            Status = Enum.Parse<EventStatus>(input.Status), StartsAtUtc = input.StartsAt.UtcDateTime, ImageUrl = input.ImageUrl,
            OrganizerId = input.OrganizerId, VenueId = input.VenueId
        };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "created");
        return CreatedAtAction(nameof(Get), new { id = item.Id }, await GetResponseAsync(item.Id));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventInput input)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        var relationError = await ValidateRelationsAsync(input);
        if (relationError is not null) return BadRequest(new { error = relationError });

        item.Title = input.Title.Trim();
        item.Description = input.Description.Trim();
        item.Category = input.Category.Trim();
        item.Status = Enum.Parse<EventStatus>(input.Status);
        item.StartsAtUtc = input.StartsAt.UtcDateTime;
        item.ImageUrl = input.ImageUrl;
        item.OrganizerId = input.OrganizerId;
        item.VenueId = input.VenueId;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "updated");
        return Ok(await GetResponseAsync(id));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        db.Events.Remove(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "deleted");
        return NoContent();
    }

    private async Task<string?> ValidateRelationsAsync(EventInput input)
    {
        if (!await db.Organizers.AnyAsync(o => o.Id == input.OrganizerId)) return "Організатора не знайдено.";
        if (!await db.Venues.AnyAsync(v => v.Id == input.VenueId)) return "Місце не знайдено.";
        return null;
    }

    private async Task<EventResponse> GetResponseAsync(int id)
    {
        var item = await db.Events.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new EventResponse(e.Id, e.Title, e.Description, e.Category, e.Status.ToString(), e.StartsAtUtc,
                e.ImageUrl, e.OrganizerId, e.Organizer.Name, e.VenueId, e.Venue.Name)).SingleAsync();
        return WithUtcKind(item);
    }

    private static EventResponse WithUtcKind(EventResponse item) =>
        item with { StartsAtUtc = DateTime.SpecifyKind(item.StartsAtUtc, DateTimeKind.Utc) };
}
