using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Api;
using MistoPodii.Data;
using MistoPodii.Models;
using MistoPodii.Services;
using MistoPodii.Realtime;

namespace MistoPodii.Controllers;

public class EventsController(AppDbContext db, EventPageCache pageCache, WeatherService weatherService, EventNotifier notifier) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Events.Include(e => e.Organizer).Include(e => e.Venue)
        .OrderBy(e => e.StartsAtUtc).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var item = await db.Events.Include(e => e.Organizer).Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (item is null) return NotFound();
        ViewData["Weather"] = await weatherService.GetForEventAsync(item, HttpContext.RequestAborted);
        return View(item);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectionsAsync();
        return View(new Event { StartsAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(15) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Category,Status,StartsAtUtc,ImageUrl,OrganizerId,VenueId")] Event item)
    {
        await ValidateRelationsAsync(item);
        if (!ModelState.IsValid)
        {
            await LoadSelectionsAsync(item);
            return View(item);
        }

        item.StartsAtUtc = DateTime.SpecifyKind(item.StartsAtUtc, DateTimeKind.Utc);
        db.Events.Add(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "created");
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        await LoadSelectionsAsync(item);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Category,Status,StartsAtUtc,ImageUrl,OrganizerId,VenueId")] Event input)
    {
        if (id != input.Id) return BadRequest();
        await ValidateRelationsAsync(input);
        if (!ModelState.IsValid)
        {
            await LoadSelectionsAsync(input);
            return View(input);
        }

        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        item.Title = input.Title;
        item.Description = input.Description;
        item.Category = input.Category;
        item.Status = input.Status;
        item.StartsAtUtc = DateTime.SpecifyKind(input.StartsAtUtc, DateTimeKind.Utc);
        item.ImageUrl = input.ImageUrl;
        item.OrganizerId = input.OrganizerId;
        item.VenueId = input.VenueId;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "updated");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Events.Include(e => e.Organizer).Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        db.Events.Remove(item);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        await notifier.PublishAsync(item, "deleted");
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateRelationsAsync(Event item)
    {
        if (item.OrganizerId > 0 && !await db.Organizers.AnyAsync(o => o.Id == item.OrganizerId))
            ModelState.AddModelError(nameof(Event.OrganizerId), "Оберіть наявного організатора.");
        if (item.VenueId > 0 && !await db.Venues.AnyAsync(v => v.Id == item.VenueId))
            ModelState.AddModelError(nameof(Event.VenueId), "Оберіть наявне місце.");
    }

    private async Task LoadSelectionsAsync(Event? item = null)
    {
        ViewBag.Organizers = new SelectList(await db.Organizers.OrderBy(o => o.Name).ToListAsync(), "Id", "Name", item?.OrganizerId);
        ViewBag.VenueName = item?.VenueId > 0
            ? await db.Venues.Where(v => v.Id == item.VenueId).Select(v => v.Name).FirstOrDefaultAsync()
            : null;
    }
}
