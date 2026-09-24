using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MistoPodii.Data;

namespace MistoPodii.Controllers;

public sealed class SeoController(AppDbContext db) : Controller
{
    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var sitemap = Url.Action(nameof(Sitemap), "Seo", values: null, protocol: Request.Scheme);
        return Content($"User-agent: *\nAllow: /\nSitemap: {sitemap}\n", "text/plain; charset=utf-8");
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<string> { Url.Action("Index", "Home", values: null, protocol: Request.Scheme)! };
        var eventIds = await db.Events.AsNoTracking().Select(e => e.Id).ToListAsync();
        urls.AddRange(eventIds.Select(id => Url.Action("Details", "Events", new { id }, Request.Scheme)!));
        var xml = new XDocument(new XElement(ns + "urlset", urls.Select(url => new XElement(ns + "url", new XElement(ns + "loc", url)))));
        return Content(xml.ToString(), "application/xml; charset=utf-8");
    }
}
