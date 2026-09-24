using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;
using MistoPodii.Models;

namespace MistoPodii.Api;

[ApiController]
[Route("api/organizers")]
public sealed class OrganizersApiController(AppDbContext db, EventPageCache pageCache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizerResponse>>> List() =>
        Ok(await db.Organizers.AsNoTracking().OrderBy(o => o.Id)
            .Select(o => new OrganizerResponse(o.Id, o.Name, o.Description, o.Contact, o.WebsiteUrl)).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizerResponse>> Get(int id)
    {
        var item = await db.Organizers.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new OrganizerResponse(o.Id, o.Name, o.Description, o.Contact, o.WebsiteUrl)).FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizerResponse>> Create(OrganizerInput input)
    {
        var item = new Organizer { Name = input.Name.Trim(), Description = input.Description.Trim(), Contact = input.Contact.Trim(), WebsiteUrl = input.WebsiteUrl };
        db.Organizers.Add(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OrganizerInput input)
    {
        var item = await db.Organizers.FindAsync(id);
        if (item is null) return NotFound();
        item.Name = input.Name.Trim();
        item.Description = input.Description.Trim();
        item.Contact = input.Contact.Trim();
        item.WebsiteUrl = input.WebsiteUrl;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Organizers.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Events.AnyAsync(e => e.OrganizerId == id))
            return Conflict(new { error = "Спершу видаліть або перенесіть події цього організатора." });
        db.Organizers.Remove(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return NoContent();
    }

    private static OrganizerResponse ToResponse(Organizer item) =>
        new(item.Id, item.Name, item.Description, item.Contact, item.WebsiteUrl);
}
