using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Infrastructure.Repositories;

/// <summary>訂單儲存庫的記憶體實作</summary>
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, Order> _orders = new();
    private int _nextId = 1001;

    public Task<Order?> GetByIdAsync(int id)
        => Task.FromResult(_orders.TryGetValue(id, out var o) ? o : null);

    public Task<IEnumerable<Order>> GetAllAsync()
        => Task.FromResult(_orders.Values.AsEnumerable());

    public Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email)
        => Task.FromResult(_orders.Values.Where(o =>
            string.Equals(o.CustomerEmail, email, StringComparison.OrdinalIgnoreCase)));

    public Task<Order> CreateAsync(Order order)
    {
        order.Id = _nextId++;
        order.CreatedAt = DateTime.UtcNow;
        _orders[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<Order?> UpdateStatusAsync(int id, OrderStatus status)
    {
        if (!_orders.TryGetValue(id, out var order)) return Task.FromResult<Order?>(null);
        order.Status = status;
        return Task.FromResult<Order?>(order);
    }
}
