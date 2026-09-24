using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Api;
using MistoPodii.Data;
using MistoPodii.Models;

namespace MistoPodii.Controllers;

public class OrganizersController(AppDbContext db, EventPageCache pageCache) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Organizers.OrderBy(o => o.Name).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var organizer = await db.Organizers.Include(o => o.Events).FirstOrDefaultAsync(o => o.Id == id);
        return organizer is null ? NotFound() : View(organizer);
    }

    public IActionResult Create() => View(new Organizer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,Contact,WebsiteUrl")] Organizer organizer)
    {
        if (!ModelState.IsValid) return View(organizer);
        db.Organizers.Add(organizer);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Details), new { id = organizer.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var organizer = await db.Organizers.FindAsync(id);
        return organizer is null ? NotFound() : View(organizer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Contact,WebsiteUrl")] Organizer input)
    {
        if (id != input.Id) return BadRequest();
        if (!ModelState.IsValid) return View(input);
        var organizer = await db.Organizers.FindAsync(id);
        if (organizer is null) return NotFound();
        organizer.Name = input.Name;
        organizer.Description = input.Description;
        organizer.Contact = input.Contact;
        organizer.WebsiteUrl = input.WebsiteUrl;
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var organizer = await db.Organizers.FindAsync(id);
        return organizer is null ? NotFound() : View(organizer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var organizer = await db.Organizers.FindAsync(id);
        if (organizer is null) return NotFound();
        if (await db.Events.AnyAsync(e => e.OrganizerId == id))
        {
            ModelState.AddModelError(string.Empty, "Спершу видаліть або перенесіть події цього організатора.");
            return View("Delete", organizer);
        }

        db.Organizers.Remove(organizer);
        await db.SaveChangesAsync();
        pageCache.Invalidate();
        return RedirectToAction(nameof(Index));
    }
}
