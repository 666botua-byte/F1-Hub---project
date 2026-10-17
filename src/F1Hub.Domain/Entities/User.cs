namespace F1Hub.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? FavoriteTeamId { get; set; }
    public Team? FavoriteTeam { get; set; }

    public int? FavoriteDriverId { get; set; }
    public Driver? FavoriteDriver { get; set; }

    public List<Article> Articles { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}
