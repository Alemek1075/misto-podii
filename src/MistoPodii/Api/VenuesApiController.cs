using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;
using MistoPodii.Models;

namespace MistoPodii.Api;

[ApiController]
[Route("api/venues")]
public sealed class VenuesApiController(AppDbContext db, EventPageCache pageCache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VenueResponse>>> List() =>
        Ok(await db.Venues.AsNoTracking().OrderBy(v => v.Id)
            .Select(v => new VenueResponse(v.Id, v.Name, v.Address, v.Latitude, v.Longitude, v.Capacity)).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VenueResponse>> Get(int id)
    {
        var item = await db.Venues.AsNoTracking().Where(v => v.Id == id)
            .Select(v => new VenueResponse(v.Id, v.Name, v.Address, v.Latitude, v.Longitude, v.Capacity)).FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<VenueResponse>> Create(VenueInput input)
    {
        var item = new Venue { Name = input.Name.Trim(), Address = input.Address.Trim(), Latitude = input.Latitude, Longitude = input.Longitude, Capacity = input.Capacity };
        db.Venues.Add(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VenueInput input)
    {
        var item = await db.Venues.FindAsync(id);
        if (item is null) return NotFound();
        item.Name = input.Name.Trim();
        item.Address = input.Address.Trim();
        item.Latitude = input.Latitude;
        item.Longitude = input.Longitude;
        item.Capacity = input.Capacity;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Venues.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Events.AnyAsync(e => e.VenueId == id))
            return Conflict(new { error = "Спершу видаліть або перенесіть події цього місця." });
        db.Venues.Remove(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return NoContent();
    }

    private static VenueResponse ToResponse(Venue item) =>
        new(item.Id, item.Name, item.Address, item.Latitude, item.Longitude, item.Capacity);
}
