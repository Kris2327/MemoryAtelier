using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "All fields are required." });

        if (dto.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var (result, error) = await authService.RegisterAsync(dto);
        if (result == null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password are required." });

        var (result, error) = await authService.LoginAsync(dto);
        if (result == null)
            return Unauthorized(new { message = error });

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        await authService.ForgotPasswordAsync(dto.Email);
        // Винаги 200 — не разкриваме дали имейлът съществува в системата.
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { message = "Token and new password are required." });

        var error = await authService.ResetPasswordAsync(dto);
        if (error != null)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await authService.GetProfileAsync(UserId);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var (result, error) = await authService.UpdateProfileAsync(UserId, dto);
        if (result == null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { message = "New password is required." });

        var error = await authService.ChangePasswordAsync(UserId, dto);
        if (error != null)
            return BadRequest(new { message = error });

        return NoContent();
    }
}