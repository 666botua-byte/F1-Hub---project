namespace F1Hub.Domain.Entities;

public class Driver
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public int? Number { get; set; }
    public string? Country { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
}
