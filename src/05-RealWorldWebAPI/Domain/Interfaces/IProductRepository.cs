using RealWorldWebAPI.Domain.Entities;

namespace RealWorldWebAPI.Domain.Interfaces;

/// <summary>
/// 產品儲存庫介面（Domain 層定義）
/// Infrastructure 層負責實作，Application 層負責使用
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync(string? category = null);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> UpdateStockAsync(int id, int quantityChange);
}
