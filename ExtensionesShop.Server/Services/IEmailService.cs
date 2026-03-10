using ExtensionesShop.Shared.Models;

namespace ExtensionesShop.Server.Services;

public interface IEmailService
{
    Task SendOrderConfirmationToCustomerAsync(Order order);
    Task SendOrderNotificationToCompanyAsync(Order order);
    Task SendContactFormAsync(string name, string email, string phone, string message);
}
