using RealWorldWebAPI.Domain.Entities;

namespace RealWorldWebAPI.Domain.Interfaces;

/// <summary>建立訂單的請求 DTO</summary>
public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    List<OrderItemRequest> Items,
    string? Notes = null
);

public record OrderItemRequest(int ProductId, int Quantity);

/// <summary>
/// 訂單服務介面（業務邏輯層）
/// 封裝訂單相關的業務規則
/// </summary>
public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Order?> GetOrderAsync(int id);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(string email);
    Task<Order?> CancelOrderAsync(int id);
    Task<Order?> UpdateOrderStatusAsync(int id, OrderStatus status);
}
