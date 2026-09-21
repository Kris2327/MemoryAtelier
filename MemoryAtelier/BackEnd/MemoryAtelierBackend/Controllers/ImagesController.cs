using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(SupabaseStorageService storageService, AppDbContext db) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Only image files are allowed." });

        var url = await storageService.UploadImageAsync(file, HttpContext.RequestAborted);
        return Ok(new { url });
    }

    // Еднократна операция за админа: тегли всяка продуктова/hero снимка, преоразмерява я ако е над
    // MaxDimension (стари качвания отпреди тази логика) и я качва обратно на СЪЩИЯ URL — идемпотентна е,
    // вече оптимизираните снимки просто се прескачат при повторно пускане.
    [HttpPost("reprocess-legacy")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReprocessLegacyImages(CancellationToken ct)
    {
        var productImageUrls = await db.Products.IgnoreQueryFilters()
            .SelectMany(p => p.Images.Select(i => i.ImageUrl))
            .ToListAsync(ct);
        var heroImageUrls = await db.HeroImages.Select(h => h.ImageUrl).ToListAsync(ct);

        var urls = productImageUrls.Concat(heroImageUrls).Distinct().ToList();

        int reprocessed = 0, alreadyOptimized = 0, skipped = 0, failed = 0;
        foreach (var url in urls)
        {
            var result = await storageService.ReprocessIfOversizedAsync(url, ct);
            switch (result)
            {
                case ImageReprocessResult.Reprocessed: reprocessed++; break;
                case ImageReprocessResult.AlreadyOptimized: alreadyOptimized++; break;
                case ImageReprocessResult.Skipped: skipped++; break;
                case ImageReprocessResult.Failed: failed++; break;
            }
        }

        return Ok(new { total = urls.Count, reprocessed, alreadyOptimized, skipped, failed });
    }
}
