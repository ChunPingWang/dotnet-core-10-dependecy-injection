using Microsoft.Extensions.Logging;
using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Application.Services;

/// <summary>
/// 訂單服務實作（Application 層）
/// 包含訂單相關的業務邏輯，依賴 Domain 介面
///
/// 展示的 DI 概念：
/// - 建構子注入多個依賴
/// - 服務之間的協作（Orchestration）
/// - 業務邏輯與資料存取的分離
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderService> _logger;

    // 建構子注入：清楚表明這個服務需要哪些依賴
    public OrderService(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IEmailService emailService,
        ILogger<OrderService> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// 建立訂單（業務邏輯）：
    /// 1. 驗證所有商品是否存在且庫存足夠
    /// 2. 建立訂單記錄
    /// 3. 扣除庫存
    /// 4. 發送確認信
    /// </summary>
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("開始處理訂單：{CustomerEmail} 的訂單", request.CustomerEmail);

        // Step 1: 驗證商品並準備訂單項目
        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);

            if (product == null)
                throw new KeyNotFoundException($"找不到商品 #{item.ProductId}");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException(
                    $"商品「{product.Name}」庫存不足（需要 {item.Quantity}，現有 {product.Stock}）");

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        // Step 2: 建立訂單
        var order = new Order
        {
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Items = orderItems,
            Status = OrderStatus.Confirmed,
            Notes = request.Notes
        };
        var createdOrder = await _orderRepo.CreateAsync(order);

        // Step 3: 扣除庫存
        foreach (var item in request.Items)
        {
            await _productRepo.UpdateStockAsync(item.ProductId, -item.Quantity);
        }

        // Step 4: 發送確認信（非同步，不阻塞回應）
        _ = _emailService.SendOrderConfirmationAsync(createdOrder);

        _logger.LogInformation(
            "訂單 #{OrderId} 建立成功，共 {ItemCount} 件，總計 NT$ {Total:N0}",
            createdOrder.Id, createdOrder.Items.Count, createdOrder.Total);

        return createdOrder;
    }

    public Task<Order?> GetOrderAsync(int id)
        => _orderRepo.GetByIdAsync(id);

    public Task<IEnumerable<Order>> GetCustomerOrdersAsync(string email)
        => _orderRepo.GetByCustomerEmailAsync(email);

    public async Task<Order?> CancelOrderAsync(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return null;

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOperationException("訂單已出貨或送達，無法取消");

        // 還原庫存
        foreach (var item in order.Items)
            await _productRepo.UpdateStockAsync(item.ProductId, item.Quantity);

        var cancelledOrder = await _orderRepo.UpdateStatusAsync(id, OrderStatus.Cancelled);

        if (cancelledOrder != null)
            _ = _emailService.SendOrderStatusUpdateAsync(cancelledOrder);

        _logger.LogInformation("訂單 #{OrderId} 已取消", id);
        return cancelledOrder;
    }

    public async Task<Order?> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        var updated = await _orderRepo.UpdateStatusAsync(id, status);
        if (updated != null)
        {
            _ = _emailService.SendOrderStatusUpdateAsync(updated);
            _logger.LogInformation("訂單 #{OrderId} 狀態更新為 {Status}", id, status);
        }
        return updated;
    }
}
