namespace RealWorldWebAPI.Domain.Entities;

/// <summary>訂單狀態</summary>
public enum OrderStatus
{
    Pending,    // 待處理
    Confirmed,  // 已確認
    Shipped,    // 已出貨
    Delivered,  // 已送達
    Cancelled   // 已取消
}

/// <summary>訂單項目</summary>
public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
}

/// <summary>訂單實體</summary>
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total => Items.Sum(i => i.Subtotal);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
