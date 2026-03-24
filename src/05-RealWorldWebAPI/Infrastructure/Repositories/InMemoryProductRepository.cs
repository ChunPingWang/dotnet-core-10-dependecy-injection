using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Infrastructure.Repositories;

/// <summary>
/// 產品儲存庫的記憶體實作
/// 在真實專案中，這裡會是 EF Core 或 Dapper 的 SQL 實作
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    // 模擬資料庫（Thread-safe 字典）
    private readonly Dictionary<int, Product> _products = new()
    {
        [1] = new() { Id = 1, Name = "MacBook Pro 14\"", Description = "Apple 筆記型電腦", Price = 75000m, Stock = 10, Category = "電腦" },
        [2] = new() { Id = 2, Name = "iPhone 16 Pro", Description = "Apple 智慧手機", Price = 38000m, Stock = 25, Category = "手機" },
        [3] = new() { Id = 3, Name = "AirPods Pro 2", Description = "Apple 無線耳機", Price = 8500m, Stock = 50, Category = "配件" },
        [4] = new() { Id = 4, Name = "iPad Air", Description = "Apple 平板電腦", Price = 20000m, Stock = 15, Category = "平板" },
        [5] = new() { Id = 5, Name = "Magic Keyboard", Description = "Apple 無線鍵盤", Price = 3500m, Stock = 30, Category = "配件" },
    };

    private int _nextId = 6;

    public Task<Product?> GetByIdAsync(int id)
        => Task.FromResult(_products.TryGetValue(id, out var p) ? p : null);

    public Task<IEnumerable<Product>> GetAllAsync(string? category = null)
    {
        var products = _products.Values.Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(category))
            products = products.Where(p => p.Category == category);
        return Task.FromResult(products);
    }

    public Task<Product> CreateAsync(Product product)
    {
        product.Id = _nextId++;
        product.CreatedAt = DateTime.UtcNow;
        _products[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(int id, Product updated)
    {
        if (!_products.ContainsKey(id)) return Task.FromResult<Product?>(null);

        updated.Id = id;
        _products[id] = updated;
        return Task.FromResult<Product?>(updated);
    }

    public Task<bool> DeleteAsync(int id)
    {
        if (!_products.ContainsKey(id)) return Task.FromResult(false);
        _products[id].IsActive = false; // 軟刪除
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(int id)
        => Task.FromResult(_products.ContainsKey(id) && _products[id].IsActive);

    public Task<bool> UpdateStockAsync(int id, int quantityChange)
    {
        if (!_products.TryGetValue(id, out var product)) return Task.FromResult(false);
        if (product.Stock + quantityChange < 0) return Task.FromResult(false); // 庫存不足

        product.Stock += quantityChange;
        return Task.FromResult(true);
    }
}
