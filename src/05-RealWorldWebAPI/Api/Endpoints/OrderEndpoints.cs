using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Api.Endpoints;

/// <summary>
/// 訂單相關 API 端點
///
/// 注意：這裡注入 IOrderService（Service 層）
/// 而非直接注入 Repository，因為訂單操作包含複雜業務邏輯
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .WithOpenApi();

        // POST /api/orders - 建立訂單
        group.MapPost("/", async (
            CreateOrderRequest request,
            IOrderService orderService,
            ILogger<Program> logger) =>
        {
            try
            {
                var order = await orderService.CreateOrderAsync(request);
                logger.LogInformation("訂單 #{OrderId} 建立成功", order.Id);
                return Results.Created($"/api/orders/{order.Id}", order);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        })
        .WithSummary("建立新訂單")
        .WithDescription("驗證庫存後建立訂單並發送確認信");

        // GET /api/orders/{id} - 取得訂單
        group.MapGet("/{id:int}", async (
            int id,
            IOrderService orderService) =>
        {
            var order = await orderService.GetOrderAsync(id);
            return order != null
                ? Results.Ok(order)
                : Results.NotFound(new { Message = $"找不到訂單 #{id}" });
        })
        .WithSummary("取得訂單詳情");

        // GET /api/orders/customer/{email} - 取得客戶的所有訂單
        group.MapGet("/customer/{email}", async (
            string email,
            IOrderService orderService) =>
        {
            var orders = await orderService.GetCustomerOrdersAsync(email);
            return Results.Ok(orders);
        })
        .WithSummary("取得客戶的訂單記錄");

        // POST /api/orders/{id}/cancel - 取消訂單
        group.MapPost("/{id:int}/cancel", async (
            int id,
            IOrderService orderService,
            ILogger<Program> logger) =>
        {
            try
            {
                var order = await orderService.CancelOrderAsync(id);
                if (order == null)
                    return Results.NotFound(new { Message = $"找不到訂單 #{id}" });

                logger.LogInformation("訂單 #{OrderId} 已取消", id);
                return Results.Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        })
        .WithSummary("取消訂單");

        // PATCH /api/orders/{id}/status - 更新訂單狀態
        group.MapPatch("/{id:int}/status", async (
            int id,
            UpdateStatusRequest request,
            IOrderService orderService) =>
        {
            var order = await orderService.UpdateOrderStatusAsync(id, request.Status);
            return order != null
                ? Results.Ok(order)
                : Results.NotFound(new { Message = $"找不到訂單 #{id}" });
        })
        .WithSummary("更新訂單狀態");
    }
}

public record UpdateStatusRequest(OrderStatus Status);
