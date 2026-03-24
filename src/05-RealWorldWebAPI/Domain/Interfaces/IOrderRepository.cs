using RealWorldWebAPI.Domain.Entities;

namespace RealWorldWebAPI.Domain.Interfaces;

/// <summary>訂單儲存庫介面</summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email);
    Task<Order> CreateAsync(Order order);
    Task<Order?> UpdateStatusAsync(int id, OrderStatus status);
}
