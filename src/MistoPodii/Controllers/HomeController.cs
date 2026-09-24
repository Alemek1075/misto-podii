using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;
using MistoPodii.Models;

namespace MistoPodii.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? category)
    {
        var query = db.Events.Include(e => e.Venue).Include(e => e.Organizer).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e => e.Title.Contains(term) || e.Description.Contains(term) || e.Venue.Name.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        ViewBag.Query = q;
        ViewBag.Category = category;
        ViewBag.Categories = await db.Events.Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync();
        return View(await query.OrderBy(e => e.StartsAtUtc).ToListAsync());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
