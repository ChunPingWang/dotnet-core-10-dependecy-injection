using Microsoft.Extensions.DependencyInjection;
using RealWorldWebAPI.Domain.Interfaces;
using RealWorldWebAPI.Infrastructure.Repositories;
using RealWorldWebAPI.Infrastructure.Services;

namespace RealWorldWebAPI.Infrastructure.Extensions;

/// <summary>
/// Infrastructure 層的服務登記擴充方法
///
/// 最佳實踐：
/// 使用 Extension Methods 把服務登記組織在各自的層中
/// 讓 Program.cs 保持簡潔，每層負責自己的服務登記
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>登記所有 Infrastructure 服務（Repository、外部服務等）</summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        // Repository 登記（Scoped：每次 HTTP 請求共用同一個實例）
        services.AddScoped<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();

        // 外部服務登記
        services.AddTransient<IEmailService, ConsoleEmailService>();

        return services;
    }
}
