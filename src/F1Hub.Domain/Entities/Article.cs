namespace F1Hub.Domain.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime? PublishedAt { get; set; }

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }
}
