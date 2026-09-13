using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    private readonly string _jwtKey = config["Jwt:Key"] ?? "MemoryAtelierSuperSecretKey2024!";
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