using ExtensionesShop.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IEmailService emailService, ILogger<OrdersController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendOrderEmail([FromBody] OrderEmailRequest request)
    {
        try
        {
            var orderData = new OrderEmailData
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal,
                    Color = i.Color,
                    Length = i.Length
                }).ToList(),
                Total = request.Total,
                OrderDate = request.OrderDate
            };

            var success = await _emailService.SendOrderEmailAsync(orderData);

            if (success)
            {
                _logger.LogInformation("Pedido enviado correctamente para {Customer}", request.CustomerEmail);
                return Ok(new { message = "Pedido enviado correctamente" });
            }
            else
            {
                return StatusCode(500, new { message = "Error al enviar el pedido" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando pedido");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

// DTOs
public class OrderEmailRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderItemRequest
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Color { get; set; }
    public string? Length { get; set; }
}
