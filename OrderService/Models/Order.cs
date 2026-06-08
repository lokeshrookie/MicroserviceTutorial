// OrderService/Models/Order.cs
namespace OrderService.Models;

/// <summary>
/// Represents a customer order. Using a class (not record) so EF Core
/// can track and update individual properties (e.g. Status).
/// </summary>
public class Order
{
    public int      Id        { get; set; }
    public int      ProductId { get; set; }
    public int      Quantity  { get; set; }
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Lifecycle status: "Placed" | "Cancelled"
    /// Soft-delete pattern — orders are never hard-deleted.
    /// </summary>
    public string Status { get; set; } = "Placed";
}
