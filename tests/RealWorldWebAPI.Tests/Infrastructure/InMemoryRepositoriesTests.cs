using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Infrastructure.Repositories;

namespace RealWorldWebAPI.Tests.Infrastructure;

public class InMemoryRepositoriesTests
{
    [Fact]
    public async Task InMemoryProductRepository_filters_active_products_and_categories()
    {
        var repository = new InMemoryProductRepository();

        await repository.DeleteAsync(5);
        var accessories = (await repository.GetAllAsync("配件")).ToList();

        Assert.All(accessories, product => Assert.Equal("配件", product.Category));
        Assert.DoesNotContain(accessories, product => product.Id == 5);
    }

    [Fact]
    public async Task InMemoryProductRepository_prevents_stock_from_becoming_negative()
    {
        var repository = new InMemoryProductRepository();

        var updated = await repository.UpdateStockAsync(1, -999);
        var product = await repository.GetByIdAsync(1);

        Assert.False(updated);
        Assert.NotNull(product);
        Assert.Equal(10, product!.Stock);
    }

    [Fact]
    public async Task InMemoryOrderRepository_assigns_ids_and_queries_customer_email_case_insensitively()
    {
        var repository = new InMemoryOrderRepository();

        var created = await repository.CreateAsync(new Order
        {
            CustomerName = "Rex",
            CustomerEmail = "rex@example.com",
            Items =
            [
                new OrderItem { ProductId = 1, ProductName = "MacBook Pro 14\"", Quantity = 1, UnitPrice = 75000m }
            ]
        });

        var customerOrders = (await repository.GetByCustomerEmailAsync("REX@example.com")).ToList();
        var updated = await repository.UpdateStatusAsync(created.Id, OrderStatus.Shipped);

        Assert.Equal(1001, created.Id);
        Assert.Single(customerOrders);
        Assert.NotNull(updated);
        Assert.Equal(OrderStatus.Shipped, updated!.Status);
    }
}
