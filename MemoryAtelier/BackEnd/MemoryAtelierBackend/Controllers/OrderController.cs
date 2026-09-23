using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController(OrderService orderService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var (result, error) = await orderService.CreateAsync(UserId, dto);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll() => Ok(await orderService.GetAllAsync());

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine() => Ok(await orderService.GetMyOrdersAsync(UserId));

    [HttpPut("{id:guid}/seen")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkSeen(Guid id)
    {
        var success = await orderService.MarkSeenAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "Status is required." });
        }

        var success = await orderService.UpdateStatusAsync(id, dto.Status.Trim(), dto.Note);
        return success ? NoContent() : NotFound();
    }

    [HttpPut("{orderId:guid}/items/{itemId:guid}/shipment-info")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateShipmentInfo(Guid orderId, Guid itemId, [FromBody] UpdateShipmentInfoDto dto)
    {
        var success = await orderService.UpdateItemShipmentInfoAsync(orderId, itemId, dto);
        return success ? NoContent() : NotFound();
    }
}
