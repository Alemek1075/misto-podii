using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;

namespace MistoPodii.Controllers;

public sealed class MapController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await db.Events.AsNoTracking().Select(e => e.Category)
            .Distinct().OrderBy(c => c).ToListAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Data(string? category, string? date)
    {
        var query = db.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                return BadRequest(new { error = "Дата має бути у форматі yyyy-MM-dd." });
            if (day == DateOnly.MaxValue) return BadRequest(new { error = "Дата поза допустимим діапазоном." });
            var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(e => e.StartsAtUtc >= start && e.StartsAtUtc < end);
        }

        var events = await query.OrderBy(e => e.StartsAtUtc).Select(e => new
        {
            e.Id, e.Title, e.Category, e.StartsAtUtc,
            e.VenueId, VenueName = e.Venue.Name,
            e.Venue.Latitude, e.Venue.Longitude
        }).ToListAsync();
        return Json(events);
    }
}
