using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(SupabaseStorageService storageService) : ControllerBase
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
}
