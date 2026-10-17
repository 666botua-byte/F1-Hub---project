namespace F1Hub.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "New";
    public decimal TotalAmount { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public List<OrderItem> Items { get; set; } = new();
}
