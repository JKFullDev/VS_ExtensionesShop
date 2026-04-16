namespace ExtensionesShop.Server.Services;

using ExtensionesShop.Shared.Models;

public interface IEmailService
{
    Task<bool> SendOrderEmailAsync(OrderEmailData orderData);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    string GetOwnerEmail();
    string GenerateContactEmailHtml(ContactFormModel form);
}

public class OrderEmailData
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Color { get; set; }
    public string? Length { get; set; }
}
