namespace F1Hub.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }

    public List<Driver> Drivers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
broken
