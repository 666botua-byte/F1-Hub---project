using System.Security.Claims;
using F1Hub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace F1Hub.Api.Endpoints;

public record SetFavoriteTeamRequest(int TeamId);

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/user/favorite-team", async (SetFavoriteTeamRequest req, AppDbContext db, ClaimsPrincipal userClaims, CancellationToken ct) =>
            {
                // Отримуємо ID користувача з JWT-токена
                var userIdString = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out var userId)) return Results.Unauthorized();

                // Знаходимо користувача в базі
                var user = await db.Users.FindAsync([userId], ct);
                if (user is null) return Results.NotFound(new { error = "Користувача не знайдено" });

                // Зберігаємо вибір команди
                user.FavoriteTeamId = req.TeamId;
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Улюблену команду успішно збережено", teamId = user.FavoriteTeamId });
            })
            .RequireAuthorization(); // Цей маршрут вимагає валідного JWT-токена
    }
}