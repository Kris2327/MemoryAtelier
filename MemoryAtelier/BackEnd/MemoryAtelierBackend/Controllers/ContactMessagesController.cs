using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ContactMessagesController(ContactMessageService contactMessageService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await contactMessageService.GetAllAsync());

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyContactMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReplyText))
        {
            return BadRequest(new { message = "Reply text is required." });
        }

        var success = await contactMessageService.ReplyAsync(id, dto.ReplyText.Trim());
        return success ? NoContent() : NotFound();
    }
}
