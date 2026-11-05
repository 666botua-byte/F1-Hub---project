using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using F1Hub.Api.Data;
using F1Hub.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace F1Hub.Api.Endpoints;

public record RegisterRequest(string? Email, string? Password, string? DisplayName);
public record LoginRequest(string? Email, string? Password);
public record ForgotPasswordRequest(string? Email);
public record ResetPasswordRequest(string? Email, string? Token, string? NewPassword);

public static class AuthEndpoints
{
    private const int BcryptWorkFactor = 11;

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static void MapAuthEndpoints(this WebApplication app)
    {
        // ---------- Реєстрація ----------
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: BcryptWorkFactor),
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

            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email, user.DisplayName });
        });

        // ---------- Логін ----------
        app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            var email = req.Email?.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

            if (user is null || string.IsNullOrEmpty(req.Password) ||
                !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var keyValue = config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(keyValue) || keyValue.Length < 32)
                return Results.Problem(title: "Server configuration error",
                    detail: "JWT key is not configured", statusCode: StatusCodes.Status500InternalServerError);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("displayName", user.DisplayName ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        });

        // ---------- Запит на скидання пароля ----------
        app.MapPost("/api/auth/forgot-password", async (ForgotPasswordRequest req, AppDbContext db,
            IWebHostEnvironment env, ILogger<Program> log, CancellationToken ct) =>
        {
            var email = req.Email?.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

            if (user is not null)
            {
                var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                user.ResetPasswordToken = HashToken(rawToken);
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
                await db.SaveChangesAsync(ct);

                // TODO: відправка листа через MailKit.
                // Поки що токен лише в логах і лише в Development.
                if (env.IsDevelopment())
                    log.LogInformation("DEV reset token for {Email}: {Token}", email, rawToken);
            }

            // Однакова відповідь для існуючого і неіснуючого email
            return Results.Ok(new { message = "Якщо email існує, на нього відправлено інструкції." });
        });

        // ---------- Скидання пароля за токеном ----------
        app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest req, AppDbContext db, CancellationToken ct) =>
        {
            if ((req.NewPassword?.Length ?? 0) < 8)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["newPassword"] = ["Password must be at least 8 characters"] });

            if (string.IsNullOrEmpty(req.Token))
                return Results.BadRequest(new { error = "Invalid or expired token" });

            var email = req.Email?.Trim().ToLowerInvariant();
            var tokenHash = HashToken(req.Token);

            var user = await db.Users.SingleOrDefaultAsync(u =>
                u.Email == email &&
                u.ResetPasswordToken == tokenHash &&
                u.ResetPasswordTokenExpiry > DateTime.UtcNow, ct);

            if (user is null)
                return Results.BadRequest(new { error = "Invalid or expired token" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: BcryptWorkFactor);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Password updated" });
        });
    }
}