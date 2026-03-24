using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Api.Endpoints;

/// <summary>
/// 產品相關 API 端點（.NET 10 Minimal API）
///
/// 展示的 DI 概念：
/// - Minimal API 中的方法參數注入
/// - 端點直接依賴 Repository（簡單 CRUD 無需額外 Service 層）
/// - 使用 ILogger<T> 進行結構化日誌
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        // GET /api/products - 取得所有產品
        group.MapGet("/", async (
            IProductRepository repo,
            ILogger<Program> logger,
            string? category = null) =>
        {
            logger.LogInformation("查詢產品列表，分類: {Category}", category ?? "全部");
            var products = await repo.GetAllAsync(category);
            return Results.Ok(products);
        })
        .WithSummary("取得所有產品")
        .WithDescription("可選擇性以分類篩選");

        // GET /api/products/{id} - 取得單一產品
        group.MapGet("/{id:int}", async (
            int id,
            IProductRepository repo,
            ILogger<Program> logger) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product == null)
            {
                logger.LogWarning("找不到產品 #{ProductId}", id);
                return Results.NotFound(new { Message = $"找不到產品 #{id}" });
            }
            return Results.Ok(product);
        })
        .WithSummary("取得指定產品");

        // POST /api/products - 新增產品
        group.MapPost("/", async (
            Product product,
            IProductRepository repo,
            ILogger<Program> logger) =>
        {
            var created = await repo.CreateAsync(product);
            logger.LogInformation("新增產品：{ProductName}（#{ProductId}）", created.Name, created.Id);
            return Results.Created($"/api/products/{created.Id}", created);
        })
        .WithSummary("新增產品");

        // PUT /api/products/{id} - 更新產品
        group.MapPut("/{id:int}", async (
            int id,
            Product product,
            IProductRepository repo,
            ILogger<Program> logger) =>
        {
            var updated = await repo.UpdateAsync(id, product);
            if (updated == null)
                return Results.NotFound(new { Message = $"找不到產品 #{id}" });

            logger.LogInformation("更新產品 #{ProductId}", id);
            return Results.Ok(updated);
        })
        .WithSummary("更新產品");

        // DELETE /api/products/{id} - 刪除產品
        group.MapDelete("/{id:int}", async (
            int id,
            IProductRepository repo,
            ILogger<Program> logger) =>
        {
            var deleted = await repo.DeleteAsync(id);
            if (!deleted)
                return Results.NotFound(new { Message = $"找不到產品 #{id}" });

            logger.LogInformation("刪除產品 #{ProductId}", id);
            return Results.NoContent();
        })
        .WithSummary("刪除產品");
    }
}
