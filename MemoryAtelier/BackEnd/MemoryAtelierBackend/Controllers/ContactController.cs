using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("contact")]
public class ContactController(EmailService emailService, ContactMessageService contactMessageService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Name, email and message are required." });
        }

        // honeypot: невидимо за реални хора поле — ако е попълнено, значи е бот, "приемаме" тихо без да пращаме имейл
        if (!string.IsNullOrWhiteSpace(dto.Website))
        {
            return Ok();
        }

        var name = dto.Name.Trim();
        var email = dto.Email.Trim();
        var message = dto.Message.Trim();

        await contactMessageService.SaveAsync(name, email, message);
        await emailService.SendContactMessageAsync(name, email, message);
        return Ok();
    }
}
