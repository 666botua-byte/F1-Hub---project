using System.ComponentModel.DataAnnotations;
using F1Hub.Api.Data;
using F1Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1Hub.Api.Endpoints;

public record RegisterRequest(string? Email, string? Password, string? DisplayName);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var email = req.Email?.Trim().ToLowerInvariant() ?? "";

            if (!new EmailAddressAttribute().IsValid(email))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["email"] = ["Invalid email"] });

            if ((req.Password?.Length ?? 0) < 8)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["password"] = ["Password must be at least 8 characters"] });

            if (await db.Users.AnyAsync(u => u.Email == email, ct))
                return Results.Conflict(new { error = "Email already registered" });

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 11),
                DisplayName = req.DisplayName
            };

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Email already registered" });
            }

            return Results.Created($"/api/users/{user.Id}",
                new { user.Id, user.Email, user.DisplayName });
        });
    }
}