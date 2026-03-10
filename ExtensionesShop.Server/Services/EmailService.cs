using ExtensionesShop.Shared.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ExtensionesShop.Server.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOrderConfirmationToCustomerAsync(Order order)
    {
        var subject = $"✅ Confirmación de Pedido #{order.Id} - Extensiones Shop";
        var body = BuildCustomerEmailBody(order);

        await SendEmailAsync(order.CustomerEmail, subject, body);
    }

    public async Task SendOrderNotificationToCompanyAsync(Order order)
    {
        var companyEmail = _configuration["Email:CompanyEmail"] ?? "info@extensionesshop.com";
        var subject = $"🔔 Nuevo Pedido #{order.Id} Recibido";
        var body = BuildCompanyEmailBody(order);

        await SendEmailAsync(companyEmail, subject, body);
    }

    public async Task SendContactFormAsync(string name, string email, string phone, string message)
    {
        var companyEmail = _configuration["Email:CompanyEmail"] ?? "info@extensionesshop.com";
        var subject = $"📧 Nuevo mensaje de contacto de {name}";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Nuevo mensaje desde el formulario de contacto</h2>
    <p><strong>Nombre:</strong> {name}</p>
    <p><strong>Email:</strong> {email}</p>
    <p><strong>Teléfono:</strong> {phone}</p>
    <p><strong>Mensaje:</strong></p>
    <p>{message}</p>
</body>
</html>";

        await SendEmailAsync(companyEmail, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"] ?? "";
            var smtpPass = _configuration["Email:SmtpPassword"] ?? "";
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "Extensiones Shop";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(to));

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email enviado a {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {To}", to);
            throw;
        }
    }

    private static string BuildCustomerEmailBody(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
        sb.AppendLine($"<h2 style='color: #d946b5;'>¡Gracias por tu pedido, {order.CustomerName}!</h2>");
        sb.AppendLine($"<p>Tu pedido <strong>#{order.Id}</strong> ha sido recibido correctamente.</p>");
        sb.AppendLine("<hr/>");
        
        sb.AppendLine("<h3>Resumen del Pedido</h3>");
        sb.AppendLine("<table border='1' cellpadding='8' style='border-collapse: collapse; width: 100%;'>");
        sb.AppendLine("<tr style='background-color: #f3f4f6;'><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th></tr>");
        
        foreach (var item in order.Items)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.ProductName}");
            if (!string.IsNullOrEmpty(item.SelectedColor))
                sb.AppendLine($"<br/><small>Color: {item.SelectedColor}</small>");
            if (item.SelectedCentimeters.HasValue)
                sb.AppendLine($"<br/><small>Largo: {item.SelectedCentimeters} cm</small>");
            sb.AppendLine("</td>");
            sb.AppendLine($"<td style='text-align: center;'>{item.Quantity}</td>");
            sb.AppendLine($"<td>${item.UnitPrice:N2}</td>");
            sb.AppendLine($"<td><strong>${item.Subtotal:N2}</strong></td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table>");
        sb.AppendLine($"<p style='text-align: right;'><strong>Subtotal:</strong> ${order.Subtotal:N2}</p>");
        sb.AppendLine($"<p style='text-align: right;'><strong>Envío:</strong> ${order.ShippingCost:N2}</p>");
        sb.AppendLine($"<p style='text-align: right; font-size: 18px; color: #d946b5;'><strong>TOTAL:</strong> ${order.Total:N2}</p>");
        
        sb.AppendLine("<hr/>");
        sb.AppendLine("<h3>Datos de Envío</h3>");
        sb.AppendLine($"<p><strong>Dirección:</strong> {order.ShippingAddress}</p>");
        sb.AppendLine($"<p><strong>Ciudad:</strong> {order.City}</p>");
        sb.AppendLine($"<p><strong>Código Postal:</strong> {order.PostalCode}</p>");
        sb.AppendLine($"<p><strong>Teléfono:</strong> {order.CustomerPhone}</p>");
        
        if (!string.IsNullOrEmpty(order.Notes))
        {
            sb.AppendLine($"<p><strong>Notas:</strong> {order.Notes}</p>");
        }
        
        sb.AppendLine("<hr/>");
        sb.AppendLine("<p>Nos pondremos en contacto contigo para coordinar el pago y el envío.</p>");
        sb.AppendLine("<p style='color: #666;'>Gracias por confiar en Extensiones Shop 💕</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private static string BuildCompanyEmailBody(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
        sb.AppendLine($"<h2 style='color: #d946b5;'>🔔 Nuevo Pedido #{order.Id}</h2>");
        sb.AppendLine($"<p><strong>Fecha:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>");
        
        sb.AppendLine("<hr/>");
        sb.AppendLine("<h3>Datos del Cliente</h3>");
        sb.AppendLine($"<p><strong>Nombre:</strong> {order.CustomerName}</p>");
        sb.AppendLine($"<p><strong>Email:</strong> {order.CustomerEmail}</p>");
        sb.AppendLine($"<p><strong>Teléfono:</strong> {order.CustomerPhone}</p>");
        sb.AppendLine($"<p><strong>Dirección:</strong> {order.ShippingAddress}, {order.City} ({order.PostalCode})</p>");
        
        sb.AppendLine("<hr/>");
        sb.AppendLine("<h3>Productos</h3>");
        sb.AppendLine("<table border='1' cellpadding='8' style='border-collapse: collapse; width: 100%;'>");
        sb.AppendLine("<tr style='background-color: #f3f4f6;'><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th></tr>");
        
        foreach (var item in order.Items)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.ProductName}");
            if (!string.IsNullOrEmpty(item.SelectedColor))
                sb.AppendLine($"<br/><small>Color: {item.SelectedColor}</small>");
            if (item.SelectedCentimeters.HasValue)
                sb.AppendLine($"<br/><small>Largo: {item.SelectedCentimeters} cm</small>");
            sb.AppendLine("</td>");
            sb.AppendLine($"<td style='text-align: center;'>{item.Quantity}</td>");
            sb.AppendLine($"<td>${item.UnitPrice:N2}</td>");
            sb.AppendLine($"<td><strong>${item.Subtotal:N2}</strong></td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table>");
        sb.AppendLine($"<p style='text-align: right;'><strong>Subtotal:</strong> ${order.Subtotal:N2}</p>");
        sb.AppendLine($"<p style='text-align: right;'><strong>Envío:</strong> ${order.ShippingCost:N2}</p>");
        sb.AppendLine($"<p style='text-align: right; font-size: 18px; color: #d946b5;'><strong>TOTAL:</strong> ${order.Total:N2}</p>");
        
        if (!string.IsNullOrEmpty(order.Notes))
        {
            sb.AppendLine("<hr/>");
            sb.AppendLine($"<p><strong>Notas del cliente:</strong> {order.Notes}</p>");
        }
        
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }
}
