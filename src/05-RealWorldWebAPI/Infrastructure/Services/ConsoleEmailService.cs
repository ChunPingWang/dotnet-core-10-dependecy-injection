using Microsoft.Extensions.Logging;
using RealWorldWebAPI.Domain.Entities;
using RealWorldWebAPI.Domain.Interfaces;

namespace RealWorldWebAPI.Infrastructure.Services;

/// <summary>
/// Email 服務的 Console 實作（開發/示範用）
/// 在真實專案中，這裡會是 SendGrid、AWS SES 等第三方服務
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        => _logger = logger;

    public Task SendOrderConfirmationAsync(Order order)
    {
        _logger.LogInformation(
            "[Email] 訂單確認信 → {Email}：訂單 #{OrderId}，金額 NT$ {Total:N0}",
            order.CustomerEmail, order.Id, order.Total);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusUpdateAsync(Order order)
    {
        _logger.LogInformation(
            "[Email] 訂單狀態更新 → {Email}：訂單 #{OrderId} 狀態變更為 {Status}",
            order.CustomerEmail, order.Id, order.Status);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string name)
    {
        _logger.LogInformation("[Email] 歡迎信 → {Email}：歡迎 {Name} 加入！", email, name);
        return Task.CompletedTask;
    }
}
