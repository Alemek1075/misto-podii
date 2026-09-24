using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;

namespace MistoPodii.Controllers;

public sealed class LiveController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Events.AsNoTracking()
        .OrderBy(e => e.StartsAtUtc).ToListAsync());
}
