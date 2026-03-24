using Microsoft.Extensions.DependencyInjection;
using RealWorldWebAPI.Application.Extensions;
using RealWorldWebAPI.Application.Services;
using RealWorldWebAPI.Domain.Interfaces;
using RealWorldWebAPI.Infrastructure.Extensions;
using RealWorldWebAPI.Infrastructure.Repositories;
using RealWorldWebAPI.Infrastructure.Services;

namespace RealWorldWebAPI.Tests.DI;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructureServices_registers_expected_services_with_expected_lifetimes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstProductRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var secondProductRepoInSameScope = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var emailService1 = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailService2 = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var productRepoFromOtherScope = secondScope.ServiceProvider.GetRequiredService<IProductRepository>();

        Assert.IsType<InMemoryProductRepository>(firstProductRepo);
        Assert.IsType<InMemoryOrderRepository>(orderRepo);
        Assert.IsType<ConsoleEmailService>(emailService1);
        Assert.Same(firstProductRepo, secondProductRepoInSameScope);
        Assert.NotSame(firstProductRepo, productRepoFromOtherScope);
        Assert.NotSame(emailService1, emailService2);
    }

    [Fact]
    public async Task AddApplicationServices_and_AddInfrastructureServices_can_resolve_order_service_end_to_end()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddInfrastructureServices()
            .AddApplicationServices();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var order = await orderService.CreateOrderAsync(
            new CreateOrderRequest(
                "Rex",
                "rex@example.com",
                [new OrderItemRequest(1, 1)]));

        var product = await productRepository.GetByIdAsync(1);

        Assert.IsType<OrderService>(orderService);
        Assert.NotNull(product);
        Assert.Equal(1001, order.Id);
        Assert.Equal(9, product!.Stock);
    }
}
