using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;

namespace MemoryAtelierBackend.Controllers;

// Извън /api префикса нарочно — sitemap.xml трябва да е достъпен на кореновия адрес на сайта (напр. memoryatelier.bg/sitemap.xml).
[ApiController]
public class SitemapController(AppDbContext db) : ControllerBase
{
    // TODO: потвърди реалния production домейн на фронтенда, ако е различен от memoryatelier.bg
    private const string SiteUrl = "https://memoryatelier.bg";

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> GetSitemap()
    {
        var products = await db.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Slug, p.CreatedAt })
            .ToListAsync();

        var entries = new List<(string Loc, DateTime LastMod, string ChangeFreq, string Priority)>
        {
            ($"{SiteUrl}/home", DateTime.UtcNow, "daily", "1.0"),
            ($"{SiteUrl}/contact", DateTime.UtcNow, "monthly", "0.4"),
            ($"{SiteUrl}/terms", DateTime.UtcNow, "yearly", "0.2"),
            ($"{SiteUrl}/sitemap", DateTime.UtcNow, "monthly", "0.2")
        };

        entries.AddRange(products.Select(p =>
            ($"{SiteUrl}/product/{p.Id}/{p.Slug}", p.CreatedAt, "weekly", "0.8")));

        var xml = new StringBuilder();
        xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        xml.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var entry in entries)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{System.Net.WebUtility.HtmlEncode(entry.Loc)}</loc>");
            xml.AppendLine($"    <lastmod>{entry.LastMod:yyyy-MM-dd}</lastmod>");
            xml.AppendLine($"    <changefreq>{entry.ChangeFreq}</changefreq>");
            xml.AppendLine($"    <priority>{entry.Priority}</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");

        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }
}
