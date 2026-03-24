using Microsoft.Extensions.DependencyInjection;
using RealWorldWebAPI.Application.Services;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Application.Extensions;

/// <summary>
/// Application 層的服務登記擴充方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>登記所有 Application 服務（業務邏輯服務）</summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
