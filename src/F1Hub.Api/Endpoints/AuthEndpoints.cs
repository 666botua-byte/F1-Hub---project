using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using F1Hub.Api.Data;
using F1Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace F1Hub.Api.Endpoints;

public record RegisterRequest(string? Email, string? Password, string? DisplayName);
public record LoginRequest(string Email, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var email = req.Email?.Trim().ToLowerInvariant() ?? "";
            if (!new EmailAddressAttribute().IsValid(email)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Invalid email"] });
            if ((req.Password?.Length ?? 0) < 8) return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["Password must be at least 8 characters"] });
            if (await db.Users.AnyAsync(u => u.Email == email, ct)) return Results.Conflict(new { error = "Email already registered" });

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 11),
                DisplayName = req.DisplayName
            };

            db.Users.Add(user);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { error = "Email already registered" }); }
            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email, user.DisplayName });
        });

        app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email, ct);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("displayName", user.DisplayName ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        });
    }
}
