using RealWorldWebAPI.Domain.Entities;

namespace RealWorldWebAPI.Domain.Interfaces;

/// <summary>Email 服務介面</summary>
public interface IEmailService
{
    Task SendOrderConfirmationAsync(Order order);
    Task SendOrderStatusUpdateAsync(Order order);
    Task SendWelcomeEmailAsync(string email, string name);
}
