using System.Net;
using System.Net.Mail;
using System.Text;
using ExtensionesShop.Shared.Models;

namespace ExtensionesShop.Server.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendOrderEmailAsync(OrderEmailData orderData)
    {
        try
        {
            // Email de destino (el de tu clienta)
            var toEmail = _config["Email:OwnerEmail"] ?? "hola@extensiones.shop";
            var fromEmail = _config["Email:FromEmail"] ?? "pedidos@extensiones.shop";
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"] ?? "";
            var smtpPass = _config["Email:SmtpPassword"] ?? "";

            var emailBody = GenerateEmailHtml(orderData);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, "Extensiones Shop"),
                Subject = $"🛍️ Nuevo Pedido - {orderData.CustomerName} - {DateTime.Now:dd/MM/yyyy HH:mm}",
                Body = emailBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            message.CC.Add(orderData.CustomerEmail); // Copia al cliente

            // Si tienes configurado SMTP (Gmail, Outlook, etc.)
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation("Email de pedido enviado correctamente a {Email}", toEmail);
                return true;
            }
            else
            {
                // Modo desarrollo: solo logear
                _logger.LogWarning("Email no configurado. Pedido:");
                _logger.LogWarning(emailBody);
                return true; // Simula éxito en desarrollo
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de pedido");
            return false;
        }
    }

    private string GenerateEmailHtml(OrderEmailData order)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; color: #333; line-height: 1.6; }");
        sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine(".header { background: #E8607A; color: white; padding: 30px 20px; text-align: center; border-radius: 10px 10px 0 0; }");
        sb.AppendLine(".content { background: #fff; padding: 30px; border: 1px solid #ddd; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { color: #E8607A; font-size: 18px; font-weight: 600; margin-bottom: 10px; border-bottom: 2px solid #E8607A; padding-bottom: 5px; }");
        sb.AppendLine(".info-row { padding: 8px 0; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine(".info-label { font-weight: 600; color: #555; display: inline-block; width: 150px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        sb.AppendLine("th { background: #F0E8EB; padding: 12px; text-align: left; font-weight: 600; }");
        sb.AppendLine("td { padding: 12px; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine(".total { background: #FDF0F3; padding: 15px; border-radius: 8px; font-size: 20px; font-weight: bold; color: #E8607A; text-align: right; margin-top: 20px; }");
        sb.AppendLine(".footer { text-align: center; padding: 20px; color: #999; font-size: 12px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='container'>");
        
        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1 style='margin:0;'>✦ Nuevo Pedido Recibido</h1>");
        sb.AppendLine($"<p style='margin:10px 0 0 0;'>Pedido realizado el {order.OrderDate:dd/MM/yyyy} a las {order.OrderDate:HH:mm}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='content'>");

        // Datos del Cliente
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>👤 Datos del Cliente</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Nombre:</span> {order.CustomerName}</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Email:</span> <a href='mailto:{order.CustomerEmail}'>{order.CustomerEmail}</a></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Teléfono:</span> {order.CustomerPhone}</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Dirección de Envío:</span> {order.ShippingAddress}</div>");
        sb.AppendLine("</div>");

        // Productos
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>🛍️ Productos del Pedido</div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Producto</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var item in order.Items)
        {
            // Construir descripción completa del producto con variantes
            var detalles = new List<string>();

            // Agregar color si existe
            if (!string.IsNullOrEmpty(item.Color))
            {
                detalles.Add(item.Color);
            }

            // Agregar longitud/centímetros si existe
            if (!string.IsNullOrEmpty(item.Length))
            {
                detalles.Add($"{item.Length}cm");
            }

            // Combinar el nombre del producto con los detalles
            var descripcionCompleta = detalles.Any()
                ? $"{item.ProductName} - {string.Join(" / ", detalles)}"
                : item.ProductName;

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td><strong>{descripcionCompleta}</strong></td>");
            sb.AppendLine($"<td style='text-align:center;'>{item.Quantity}</td>");
            sb.AppendLine($"<td style='text-align:right;'>{item.UnitPrice:N2}€</td>");
            sb.AppendLine($"<td style='text-align:right;'><strong>{item.Subtotal:N2}€</strong></td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</div>");

        // ✅ Desglose de Costos
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>💰 Desglose de Costos</div>");
        sb.AppendLine($"<div class='info-row' style='font-size: 16px;'><span class='info-label'>Subtotal:</span> <span style='text-align: right; width: calc(100% - 150px);'>{order.Subtotal:N2}€</span></div>");

        if (order.ShippingCost > 0)
        {
            sb.AppendLine($"<div class='info-row' style='font-size: 16px;'><span class='info-label'>Envío:</span> <span style='text-align: right; width: calc(100% - 150px);'>{order.ShippingCost:N2}€</span></div>");
        }
        else
        {
            sb.AppendLine($"<div class='info-row' style='font-size: 16px;'><span class='info-label'>Envío:</span> <span style='text-align: right; width: calc(100% - 150px); color: #059669; font-weight: bold;'>GRATIS ✓</span></div>");
        }

        sb.AppendLine("</div>");

        // Total
        sb.AppendLine($"<div class='total'>TOTAL: {order.Total:N2}€</div>");

        sb.AppendLine("</div>"); // content

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>Este email se generó automáticamente desde extensiones.shop</p>");
        sb.AppendLine("<p>Responde al email del cliente para confirmar el pedido y gestionar el pago.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>"); // container
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Genera el HTML corporativo para emails de contacto
    /// </summary>
    public string GenerateContactEmailHtml(ContactFormModel form)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; color: #333; line-height: 1.6; }");
        sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine(".header { background: #E8607A; color: white; padding: 30px 20px; text-align: center; border-radius: 10px 10px 0 0; }");
        sb.AppendLine(".content { background: #fff; padding: 30px; border: 1px solid #ddd; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { color: #E8607A; font-size: 18px; font-weight: 600; margin-bottom: 10px; border-bottom: 2px solid #E8607A; padding-bottom: 5px; }");
        sb.AppendLine(".info-row { padding: 8px 0; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine(".info-label { font-weight: 600; color: #555; display: inline-block; width: 120px; }");
        sb.AppendLine(".info-value { color: #333; }");
        sb.AppendLine(".message-box { background: #FDF0F3; padding: 20px; border-left: 4px solid #E8607A; border-radius: 4px; margin-top: 15px; }");
        sb.AppendLine(".footer { text-align: center; padding: 20px; color: #999; font-size: 12px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='container'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1 style='margin:0;'>✦ Nuevo Mensaje de Contacto</h1>");
        sb.AppendLine($"<p style='margin:10px 0 0 0;'>Recibido el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='content'>");

        // Datos del Remitente
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>👤 Datos del Remitente</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Nombre:</span> <span class='info-value'>{EscapeHtml(form.Nombre)}</span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Email:</span> <span class='info-value'><a href='mailto:{form.Email}'>{form.Email}</a></span></div>");
        if (!string.IsNullOrEmpty(form.Telefono))
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Teléfono:</span> <span class='info-value'>{EscapeHtml(form.Telefono)}</span></div>");
        sb.AppendLine("</div>");

        // Asunto
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>📋 Asunto</div>");
        var asuntoTexto = ObtenerTextoAsunto(form.Asunto);
        sb.AppendLine($"<p style='margin: 0; color: #E8607A; font-weight: 600;'>{asuntoTexto}</p>");
        sb.AppendLine("</div>");

        // Mensaje
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>💬 Mensaje</div>");
        sb.AppendLine("<div class='message-box'>");
        sb.AppendLine($"<p style='margin: 0; white-space: pre-wrap;'>{EscapeHtml(form.Mensaje)}</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>"); // content

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>Este email se generó automáticamente desde ExtensionesShop.com</p>");
        sb.AppendLine("<p>Responde directamente a este email para contactar con el remitente.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>"); // container
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Escapa caracteres especiales de HTML
    /// </summary>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Convierte el código de asunto a texto descriptivo
    /// </summary>
    private static string ObtenerTextoAsunto(string asunto)
    {
        return asunto switch
        {
            "pedido" => "Consulta sobre Pedido",
            "producto" => "Información de Producto",
            "devolucion" => "Devolución/Cambio",
            _ => asunto
        };
    }

    // Método genérico para enviar emails
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var fromEmail = _config["Email:FromEmail"] ?? "noreply@extensionesshop.com";
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"] ?? "";
            var smtpPass = _config["Email:SmtpPassword"] ?? "";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, "Extensiones Shop"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            // Si tienes configurado SMTP
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation("Email enviado correctamente a {Email}", toEmail);
                return true;
            }
            else
            {
                // Modo desarrollo: solo logear
                _logger.LogWarning("Email no configurado. Contenido:");
                _logger.LogWarning("To: {Email}", toEmail);
                _logger.LogWarning("Subject: {Subject}", subject);
                _logger.LogWarning("Body: {Body}", htmlBody);
                return true; // En desarrollo, consideramos exitoso
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email a {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Obtiene el email del propietario desde configuración
    /// </summary>
    public string GetOwnerEmail()
    {
        return _config["Email:OwnerEmail"] ?? "juancarlosah.daw@gmail.com";
    }
}
