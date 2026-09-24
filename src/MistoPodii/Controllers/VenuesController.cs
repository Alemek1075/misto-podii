using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Api;
using MistoPodii.Data;
using MistoPodii.Models;

namespace MistoPodii.Controllers;

public class VenuesController(AppDbContext db, EventPageCache pageCache) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            return Json(Array.Empty<object>());

        var term = q.Trim();
        if (term.Length > 100) return BadRequest();
        var matches = await db.Venues.AsNoTracking().Where(v => v.Name.Contains(term))
            .OrderBy(v => v.Name).Take(10).Select(v => new { id = v.Id, text = v.Name }).ToListAsync();
        return Json(matches);
    }

    public async Task<IActionResult> Index() => View(await db.Venues.OrderBy(v => v.Name).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var venue = await db.Venues.Include(v => v.Events).FirstOrDefaultAsync(v => v.Id == id);
        return venue is null ? NotFound() : View(venue);
    }

    public IActionResult Create() => View(new Venue());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Address,Latitude,Longitude,Capacity")] Venue venue)
    {
        if (!ModelState.IsValid) return View(venue);
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Details), new { id = venue.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var venue = await db.Venues.FindAsync(id);
        return venue is null ? NotFound() : View(venue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Latitude,Longitude,Capacity")] Venue input)
    {
        if (id != input.Id) return BadRequest();
        if (!ModelState.IsValid) return View(input);
        var venue = await db.Venues.FindAsync(id);
        if (venue is null) return NotFound();
        venue.Name = input.Name;
        venue.Address = input.Address;
        venue.Latitude = input.Latitude;
        venue.Longitude = input.Longitude;
        venue.Capacity = input.Capacity;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var venue = await db.Venues.FindAsync(id);
        return venue is null ? NotFound() : View(venue);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var venue = await db.Venues.FindAsync(id);
        if (venue is null) return NotFound();
        if (await db.Events.AnyAsync(e => e.VenueId == id))
        {
            ModelState.AddModelError(string.Empty, "Спершу видаліть або перенесіть події цього місця.");
            return View("Delete", venue);
        }

        db.Venues.Remove(venue);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Index));
    }
}
