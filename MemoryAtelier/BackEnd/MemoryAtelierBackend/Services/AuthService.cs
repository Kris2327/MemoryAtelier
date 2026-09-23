using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class AuthService(AppDbContext db, IConfiguration config, EmailService emailService, ILogger<AuthService> logger)
{
    private readonly string _jwtKey = config["Jwt:Key"] ?? "MemoryAtelierSuperSecretKey2024!";
    private readonly string _frontendBaseUrl = config["Frontend:BaseUrl"] ?? "http://localhost:4200";
    private const string AdminEmail = "memoryatelier25@gmail.com";

    public async Task<(AuthResponseDto? Result, string? Error)> RegisterAsync(RegisterDto dto)
    {
        // Не позволявай регистрация с admin email
        if (dto.Email.ToLower() == AdminEmail.ToLower())
            return (null, "This email is not allowed for registration.");

        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return (null, "Email already in use.");

        // Позволи само един Admin
        if (await db.Users.AnyAsync(u => u.Role == "Admin"))
        {
            // Admin вече съществува — нов потребител е винаги Client
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Client",
            PhoneNumber = dto.PhoneNumber,
            BirthDate = dto.BirthDate.HasValue
    ? DateTime.SpecifyKind(dto.BirthDate.Value, DateTimeKind.Utc)
    : null,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (new AuthResponseDto(GenerateToken(user), user.Role, user.Name, user.Id, user.PhoneNumber), null);

    }

    public async Task<(AuthResponseDto? Result, string? Error)> LoginAsync(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, "Invalid email or password.");

        return (new AuthResponseDto(GenerateToken(user), user.Role, user.Name, user.Id, user.PhoneNumber), null);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId);
        return user == null ? null : ToProfileDto(user);
    }

    public async Task<(UserProfileDto? Result, string? Error)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await db.Users.FindAsync(userId);
        if (user == null) return (null, "User not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return (null, "Name is required.");

        user.Name = dto.Name.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.City = dto.City;
        user.Address = dto.Address;
        user.PostCode = dto.PostCode;

        await db.SaveChangesAsync();
        return (ToProfileDto(user), null);
    }

    public async Task<string?> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await db.Users.FindAsync(userId);
        if (user == null) return "User not found.";

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return "Current password is incorrect.";

        if (dto.NewPassword.Length < 6)
            return "Password must be at least 6 characters.";

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await db.SaveChangesAsync();

        try
        {
            await emailService.SendPasswordChangedAsync(user.Name, user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password changed notification email to {Email}.", user.Email);
        }

        return null;
    }

    // Винаги успешен резултат навън, независимо дали имейлът съществува — не издаваме кои имейли са регистрирани.
    public async Task ForgotPasswordAsync(string email)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return;

        var token = GenerateResetToken();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Used = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var resetLink = $"{_frontendBaseUrl.TrimEnd('/')}/reset-password?token={token}";

        try
        {
            await emailService.SendPasswordResetAsync(user.Name, user.Email, resetLink);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}.", user.Email);
        }
    }

    public async Task<string?> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword.Length < 6)
            return "Password must be at least 6 characters.";

        var resetToken = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == dto.Token);
        if (resetToken == null || resetToken.Used || resetToken.ExpiresAt < DateTime.UtcNow)
            return "This reset link is invalid or has expired.";

        var user = await db.Users.FindAsync(resetToken.UserId);
        if (user == null) return "User not found.";

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        resetToken.Used = true;
        await db.SaveChangesAsync();

        try
        {
            await emailService.SendPasswordChangedAsync(user.Name, user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password changed notification email to {Email}.", user.Email);
        }

        return null;
    }

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static UserProfileDto ToProfileDto(User user) =>
        new(user.Id, user.Name, user.Email, user.PhoneNumber, user.City, user.Address, user.PostCode);

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}