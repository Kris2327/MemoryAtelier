using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavouritesController(FavouritesService favouritesService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetFavourites() => Ok(await favouritesService.GetFavouritesAsync(UserId));

    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> Toggle(Guid productId)
    {
        var added = await favouritesService.ToggleFavouriteAsync(UserId, productId);
        return Ok(new { added });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount() => Ok(await favouritesService.GetFavouritesCountAsync(UserId));
}