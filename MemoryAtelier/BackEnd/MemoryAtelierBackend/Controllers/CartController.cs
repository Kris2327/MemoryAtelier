using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController(CartService cartService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart() => Ok(await cartService.GetCartAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto) =>
        Ok(await cartService.AddToCartAsync(UserId, dto));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCart(Guid id, [FromBody] UpdateCartDto dto)
    {
        var success = await cartService.UpdateCartAsync(UserId, id, dto);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveFromCart(Guid id)
    {
        var success = await cartService.RemoveFromCartAsync(UserId, id);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount() => Ok(await cartService.GetCartCountAsync(UserId));
}