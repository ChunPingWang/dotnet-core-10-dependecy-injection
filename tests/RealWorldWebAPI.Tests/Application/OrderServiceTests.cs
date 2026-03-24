using Microsoft.Extensions.Logging.Abstractions;
using RealWorldWebAPI.Application.Services;
using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Tests.Application;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_creates_order_updates_stock_and_sends_confirmation()
    {
        var productRepo = new FakeProductRepository(
            new Product { Id = 1, Name = "鍵盤", Price = 3500m, Stock = 5, Category = "配件" });
        var orderRepo = new FakeOrderRepository();
        var emailService = new RecordingEmailService();
        var service = CreateService(orderRepo, productRepo, emailService);

        var request = new CreateOrderRequest(
            "Rex",
            "rex@example.com",
            [new OrderItemRequest(1, 2)],
            "請儘速出貨");

        var order = await service.CreateOrderAsync(request);

        Assert.Equal(1001, order.Id);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(7000m, order.Total);
        Assert.Equal(3, productRepo.Products[1].Stock);
        Assert.Single(orderRepo.CreatedOrders);
        Assert.Single(emailService.ConfirmationOrders);
        Assert.Equal(order.Id, emailService.ConfirmationOrders[0].Id);
    }

    [Fact]
    public async Task CreateOrderAsync_throws_when_product_is_missing()
    {
        var service = CreateService(
            new FakeOrderRepository(),
            new FakeProductRepository(),
            new RecordingEmailService());

        var request = new CreateOrderRequest(
            "Rex",
            "rex@example.com",
            [new OrderItemRequest(99, 1)]);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateOrderAsync(request));

        Assert.Contains("找不到商品 #99", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_throws_when_stock_is_insufficient()
    {
        var productRepo = new FakeProductRepository(
            new Product { Id = 1, Name = "MacBook Pro 14\"", Price = 75000m, Stock = 1, Category = "電腦" });
        var service = CreateService(
            new FakeOrderRepository(),
            productRepo,
            new RecordingEmailService());

        var request = new CreateOrderRequest(
            "Rex",
            "rex@example.com",
            [new OrderItemRequest(1, 2)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Contains("庫存不足", exception.Message);
        Assert.Equal(1, productRepo.Products[1].Stock);
    }

    [Fact]
    public async Task CancelOrderAsync_restores_stock_updates_status_and_sends_notification()
    {
        var existingOrder = new Order
        {
            Id = 1001,
            CustomerName = "Rex",
            CustomerEmail = "rex@example.com",
            Status = OrderStatus.Confirmed,
            Items =
            [
                new OrderItem { ProductId = 1, ProductName = "iPad Air", UnitPrice = 20000m, Quantity = 2 }
            ]
        };

        var orderRepo = new FakeOrderRepository(existingOrder);
        var productRepo = new FakeProductRepository(
            new Product { Id = 1, Name = "iPad Air", Price = 20000m, Stock = 3, Category = "平板" });
        var emailService = new RecordingEmailService();
        var service = CreateService(orderRepo, productRepo, emailService);

        var cancelledOrder = await service.CancelOrderAsync(existingOrder.Id);

        Assert.NotNull(cancelledOrder);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder!.Status);
        Assert.Equal(5, productRepo.Products[1].Stock);
        Assert.Single(emailService.StatusUpdateOrders);
        Assert.Equal(existingOrder.Id, emailService.StatusUpdateOrders[0].Id);
    }

    [Fact]
    public async Task CancelOrderAsync_throws_when_order_has_already_shipped()
    {
        var shippedOrder = new Order
        {
            Id = 1001,
            CustomerName = "Rex",
            CustomerEmail = "rex@example.com",
            Status = OrderStatus.Shipped,
            Items =
            [
                new OrderItem { ProductId = 1, ProductName = "iPhone 16 Pro", UnitPrice = 38000m, Quantity = 1 }
            ]
        };

        var productRepo = new FakeProductRepository(
            new Product { Id = 1, Name = "iPhone 16 Pro", Price = 38000m, Stock = 8, Category = "手機" });
        var service = CreateService(
            new FakeOrderRepository(shippedOrder),
            productRepo,
            new RecordingEmailService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelOrderAsync(shippedOrder.Id));
        Assert.Equal(8, productRepo.Products[1].Stock);
    }

    private static OrderService CreateService(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IEmailService emailService)
        => new(orderRepo, productRepo, emailService, NullLogger<OrderService>.Instance);

    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        public Dictionary<int, Product> Products { get; } =
            products.ToDictionary(product => product.Id, CloneProduct);

        public Task<Product?> GetByIdAsync(int id)
            => Task.FromResult(Products.TryGetValue(id, out var product) ? product : null);

        public Task<IEnumerable<Product>> GetAllAsync(string? category = null)
        {
            IEnumerable<Product> query = Products.Values.Where(product => product.IsActive);
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(product => product.Category == category);
            }

            return Task.FromResult(query);
        }

        public Task<Product> CreateAsync(Product product)
        {
            Products[product.Id] = CloneProduct(product);
            return Task.FromResult(Products[product.Id]);
        }

        public Task<Product?> UpdateAsync(int id, Product product)
        {
            if (!Products.ContainsKey(id))
            {
                return Task.FromResult<Product?>(null);
            }

            product.Id = id;
            Products[id] = CloneProduct(product);
            return Task.FromResult<Product?>(Products[id]);
        }

        public Task<bool> DeleteAsync(int id)
        {
            if (!Products.TryGetValue(id, out var product))
            {
                return Task.FromResult(false);
            }

            product.IsActive = false;
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(int id)
            => Task.FromResult(Products.TryGetValue(id, out var product) && product.IsActive);

        public Task<bool> UpdateStockAsync(int id, int quantityChange)
        {
            if (!Products.TryGetValue(id, out var product) || product.Stock + quantityChange < 0)
            {
                return Task.FromResult(false);
            }

            product.Stock += quantityChange;
            return Task.FromResult(true);
        }

        private static Product CloneProduct(Product product)
            => new()
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CreatedAt = product.CreatedAt,
                IsActive = product.IsActive
            };
    }

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        private readonly Dictionary<int, Order> _orders = orders.ToDictionary(order => order.Id, CloneOrder);
        private int _nextId = orders.Length == 0 ? 1001 : orders.Max(order => order.Id) + 1;

        public List<Order> CreatedOrders { get; } = [];

        public Task<Order?> GetByIdAsync(int id)
            => Task.FromResult(_orders.TryGetValue(id, out var order) ? order : null);

        public Task<IEnumerable<Order>> GetAllAsync()
            => Task.FromResult(_orders.Values.AsEnumerable());

        public Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email)
            => Task.FromResult(_orders.Values.Where(order =>
                string.Equals(order.CustomerEmail, email, StringComparison.OrdinalIgnoreCase)));

        public Task<Order> CreateAsync(Order order)
        {
            var created = CloneOrder(order);
            created.Id = _nextId++;
            _orders[created.Id] = created;
            CreatedOrders.Add(created);
            return Task.FromResult(created);
        }

        public Task<Order?> UpdateStatusAsync(int id, OrderStatus status)
        {
            if (!_orders.TryGetValue(id, out var order))
            {
                return Task.FromResult<Order?>(null);
            }

            order.Status = status;
            return Task.FromResult<Order?>(order);
        }

        private static Order CloneOrder(Order order)
            => new()
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                Notes = order.Notes,
                Items = order.Items
                    .Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity
                    })
                    .ToList()
            };
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public List<Order> ConfirmationOrders { get; } = [];
        public List<Order> StatusUpdateOrders { get; } = [];
        public List<(string Email, string Name)> WelcomeEmails { get; } = [];

        public Task SendOrderConfirmationAsync(Order order)
        {
            ConfirmationOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task SendOrderStatusUpdateAsync(Order order)
        {
            StatusUpdateOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task SendWelcomeEmailAsync(string email, string name)
        {
            WelcomeEmails.Add((email, name));
            return Task.CompletedTask;
        }
    }
}
