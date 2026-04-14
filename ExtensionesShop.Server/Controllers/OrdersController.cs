using ExtensionesShop.Server.Data;
using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext db, IEmailService emailService, ILogger<OrdersController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    // GET api/orders
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out var currentUserId))
                userId = currentUserId; // Forzamos a que busque su propio ID
            else
                return Unauthorized();
        }

        var query = _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(items);
    }

    // GET api/orders/{id}
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        // 1. Si no existe en la base de datos
        if (order is null)
            return NotFound();

        // 2. Si existe, verificamos que el pedido sea tuyo (salvo que seas Admin)
        if (!User.IsInRole("Admin") && order.UserId.ToString() != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        {
            return Forbid(); // 403: Prohibido husmear pedidos ajenos
        }

        // 3. Si existe y tienes permiso para verlo
        return Ok(order);
    }

    // POST api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Crear el pedido
            var order = new Order
            {
                UserId = request.UserId,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress,
                City = request.City,
                PostalCode = request.PostalCode,
                Subtotal = request.Subtotal,
                ShippingCost = request.ShippingCost,
                Total = request.Total,
                Status = 0, // Pending
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // Agregar los items
            foreach (var item in request.Items)
            {
                var product = await _db.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    return BadRequest(new { message = $"Producto {item.ProductId} no encontrado" });

                if (product.Stock < item.Quantity)
                    return BadRequest(new { message = $"Stock insuficiente para {product.Name}" });

                order.OrderItems.Add(new ExtensionesShop.Shared.Models.OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity,
                    SelectedColor = item.SelectedColor,
                    SelectedCentimeters = item.SelectedCentimeters
                });

                // Reducir stock de la variante específica
                if (product.Variants.Any())
                {
                    // Buscar la variante que coincida con color y centimeters
                    var variant = product.Variants.FirstOrDefault(v =>
                        v.Color == item.SelectedColor &&
                        v.Centimeters == item.SelectedCentimeters);

                    if (variant != null && variant.Stock >= item.Quantity)
                    {
                        variant.Stock -= item.Quantity;
                    }
                    else if (variant == null)
                    {
                        // Si no encuentra una variante exacta, buscar la primera disponible
                        variant = product.Variants.FirstOrDefault(v => v.Stock >= item.Quantity);
                        if (variant != null)
                            variant.Stock -= item.Quantity;
                    }
                }
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Enviar email
            try
            {
                var emailData = new OrderEmailData
                {
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    CustomerPhone = order.CustomerPhone,
                    ShippingAddress = $"{order.ShippingAddress}, {order.City}, {order.PostalCode}",
                    Items = order.OrderItems.Select(oi => new ExtensionesShop.Server.Services.OrderItem
                    {
                        ProductName = oi.ProductName,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Subtotal = oi.UnitPrice * oi.Quantity,
                        Color = oi.SelectedColor,
                        Length = oi.SelectedCentimeters?.ToString()
                    }).ToList(),
                    Total = order.Total,
                    OrderDate = order.CreatedAt
                };

                await _emailService.SendOrderEmailAsync(emailData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de confirmación para pedido {OrderId}", order.Id);
                // No fallar la creación del pedido si falla el email
            }

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando pedido");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // PUT api/orders/{id}/status
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<ActionResult<Order>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = request.Status;

        if (request.Status == 3) // Shipped
            order.ShippedAt = DateTime.UtcNow;
        else if (request.Status == 4) // Delivered
            order.DeliveredAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(order);
    }

    /// <summary>
    /// POST /api/orders/send-email - Enviar email con los detalles del pedido (para guests)
    /// También guarda el pedido en la base de datos
    /// </summary>
    [HttpPost("send-email")]
    public async Task<IActionResult> SendOrderEmail([FromBody] OrderEmailRequest request)
    {
        try
        {
            _logger.LogInformation("📧 Recibiendo pedido de: {Email}", request.CustomerEmail);

            // Validar datos básicos
            if (string.IsNullOrEmpty(request.CustomerEmail) ||
                string.IsNullOrEmpty(request.CustomerName) ||
                request.Items == null || !request.Items.Any())
            {
                return BadRequest(new { message = "Datos del pedido incompletos" });
            }

            // ✅ OBTENER userId si está autenticado
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                    _logger.LogInformation("✅ Usuario autenticado: {UserId}", userId);
                }
            }

            // ✅ CREAR PEDIDO EN BASE DE DATOS
            var order = new Order
            {
                UserId = userId,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress,
                City = ExtractCity(request.ShippingAddress),
                PostalCode = ExtractPostalCode(request.ShippingAddress),
                Subtotal = request.Total,
                ShippingCost = 0,
                Total = request.Total,
                Status = 0, // Pendiente
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // Agregar los items del pedido
            foreach (var item in request.Items)
            {
                order.OrderItems.Add(new ExtensionesShop.Shared.Models.OrderItem
                {
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    SelectedColor = item.Color,
                    SelectedCentimeters = decimal.TryParse(item.Length, out var length) ? length : (decimal?)null
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Pedido #{OrderId} guardado en BD (UserId: {UserId})", order.Id, userId?.ToString() ?? "Guest");

            // ========================================
            // EMAIL PARA EL CLIENTE (Amigable)
            // ========================================
            var clientSubject = $"✅ Pedido Confirmado #{order.Id}";
            var clientBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #D64670;'>¡Gracias por tu pedido, {request.CustomerName.Split(' ')[0]}! 💖</h2>
                    <p>Hemos recibido tu pedido <strong>#{order.Id}</strong> correctamente.</p>

                    <div style='background: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0;'>📦 Detalles de tu Pedido:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <thead>
                                <tr style='background: #f0f0f0;'>
                                    <th style='padding: 10px; text-align: left;'>Producto</th>
                                    <th style='padding: 10px; text-align: center;'>Cantidad</th>
                                    <th style='padding: 10px; text-align: right;'>Precio</th>
                                </tr>
                            </thead>
                            <tbody>
            ";

            foreach (var item in request.Items)
            {
                var details = "";
                if (!string.IsNullOrEmpty(item.Color)) details += $" - {item.Color}";
                if (!string.IsNullOrEmpty(item.Length)) details += $" - {item.Length}cm";

                clientBody += $@"
                                <tr style='border-bottom: 1px solid #eee;'>
                                    <td style='padding: 10px;'>{item.ProductName}{details}</td>
                                    <td style='padding: 10px; text-align: center;'>{item.Quantity}</td>
                                    <td style='padding: 10px; text-align: right;'>${item.Subtotal:N2}</td>
                                </tr>
                ";
            }

            clientBody += $@"
                            </tbody>
                            <tfoot>
                                <tr style='background: #f0f0f0; font-weight: bold;'>
                                    <td colspan='2' style='padding: 10px; text-align: right;'>TOTAL:</td>
                                    <td style='padding: 10px; text-align: right; color: #D64670; font-size: 18px;'>${request.Total:N2}</td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>

                    <div style='background: #fff3f5; padding: 15px; border-left: 4px solid #D64670; margin: 20px 0;'>
                        <h4 style='margin-top: 0;'>📍 Dirección de Envío:</h4>
                        <p style='margin: 5px 0;'>{request.ShippingAddress}</p>
                    </div>

                    {(string.IsNullOrEmpty(request.Notes) ? "" : $@"
                    <div style='background: #f9f9f9; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h4 style='margin-top: 0;'>📝 Tus Notas:</h4>
                        <p style='margin: 5px 0;'>{request.Notes}</p>
                    </div>
                    ")}

                    <h3>¿Qué sigue?</h3>
                    <ol style='line-height: 1.8;'>
                        <li>Nuestro equipo revisará tu pedido</li>
                        <li>Te contactaremos en las próximas horas para coordinar el pago</li>
                        <li>Una vez confirmado el pago, enviaremos tu pedido en 24-48h</li>
                    </ol>

                    <p style='margin-top: 30px;'>Si tienes alguna pregunta, responde a este email o contáctanos:</p>
                    <p style='margin: 5px 0;'>📧 Email: <a href='mailto:juancarlosah.daw@gmail.com'>juancarlosah.daw@gmail.com</a></p>
                    <p style='margin: 5px 0;'>📱 Teléfono: {request.CustomerPhone}</p>

                    <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                    <p style='color: #999; font-size: 12px; text-align: center;'>
                        Extensiones Shop - Extensiones de Cabello Premium<br>
                        Fecha del pedido: {request.OrderDate:dd/MM/yyyy HH:mm}
                    </p>
                </div>
            ";

            // ========================================
            // EMAIL PARA LA EMPRESA (Mejorado con colores)
            // ========================================
            var ownerEmail = _emailService.GetOwnerEmail();
            var adminSubject = $"🔔 Nuevo Pedido #{order.Id} - {request.CustomerName}";
            var adminBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 700px; margin: 0 auto; background: #f5f5f5; padding: 20px;'>
                    <div style='background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <div style='background: linear-gradient(135deg, #D64670, #E85D88); color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0; font-size: 24px;'>📦 Nuevo Pedido Recibido</h2>
                            <p style='margin: 10px 0 0 0; font-size: 16px;'>Pedido #<strong>{order.Id}</strong></p>
                        </div>

                        <div style='background: #FFF3F8; border-left: 4px solid #D64670; padding: 15px; margin-bottom: 20px;'>
                            <p style='margin: 0; color: #D64670; font-weight: bold; font-size: 14px;'>⚠️ ACCIÓN REQUERIDA</p>
                            <p style='margin: 5px 0 0 0;'>Contactar al cliente para coordinar el pago</p>
                        </div>

                        <table style='width: 100%; margin-bottom: 20px;'>
                            <tr><td colspan='2' style='padding: 10px 0; border-bottom: 2px solid #D64670; font-weight: bold; color: #D64670;'>👤 DATOS DEL CLIENTE</td></tr>
                            <tr><td style='padding: 8px 0; font-weight: bold; width: 120px;'>Nombre:</td><td>{request.CustomerName}</td></tr>
                            <tr style='background: #f9f9f9;'><td style='padding: 8px 0; font-weight: bold;'>Email:</td><td><a href='mailto:{request.CustomerEmail}' style='color: #D64670;'>{request.CustomerEmail}</a></td></tr>
                            <tr><td style='padding: 8px 0; font-weight: bold;'>Teléfono:</td><td><a href='tel:{request.CustomerPhone}' style='color: #D64670;'>{request.CustomerPhone}</a></td></tr>
                            <tr style='background: #f9f9f9;'><td style='padding: 8px 0; font-weight: bold;'>Dirección:</td><td>{request.ShippingAddress}</td></tr>
                            <tr><td style='padding: 8px 0; font-weight: bold;'>Fecha:</td><td>{request.OrderDate:dd/MM/yyyy HH:mm}</td></tr>
                            <tr style='background: #f9f9f9;'><td style='padding: 8px 0; font-weight: bold;'>Usuario:</td><td>{(userId.HasValue ? $"ID: {userId}" : "Invitado")}</td></tr>
                        </table>

                        <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                            <thead>
                                <tr style='background: #D64670; color: white;'>
                                    <th style='padding: 12px; text-align: left; border-radius: 8px 0 0 0;'>Producto</th>
                                    <th style='padding: 12px; text-align: center;'>Color</th>
                                    <th style='padding: 12px; text-align: center;'>Longitud</th>
                                    <th style='padding: 12px; text-align: center;'>Cant.</th>
                                    <th style='padding: 12px; text-align: right;'>P. Unit.</th>
                                    <th style='padding: 12px; text-align: right; border-radius: 0 8px 0 0;'>Subtotal</th>
                                </tr>
                            </thead>
                            <tbody>
            ";

            foreach (var item in request.Items)
            {
                adminBody += $@"
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 12px;'>{item.ProductName}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Color ?? "-"}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Length ?? "-"}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Quantity}</td>
                            <td style='padding: 12px; text-align: right;'>${item.UnitPrice:N2}</td>
                            <td style='padding: 12px; text-align: right; font-weight: bold;'>${item.Subtotal:N2}</td>
                        </tr>
                ";
            }

            adminBody += $@"
                            </tbody>
                            <tfoot>
                                <tr style='background: #FFF3F8;'>
                                    <td colspan='5' style='padding: 15px; text-align: right; font-weight: bold; font-size: 16px; color: #D64670;'>TOTAL:</td>
                                    <td style='padding: 15px; text-align: right; font-weight: bold; font-size: 20px; color: #D64670;'>${request.Total:N2}</td>
                                </tr>
                            </tfoot>
                        </table>

                        {(string.IsNullOrEmpty(request.Notes) ? "" : $@"
                        <div style='background: #f9f9f9; padding: 15px; border-radius: 8px; margin-bottom: 20px;'>
                            <h4 style='margin: 0 0 10px 0; color: #D64670;'>📝 Notas del Cliente:</h4>
                            <p style='margin: 0;'>{request.Notes}</p>
                        </div>
                        ")}

                        <div style='background: #E8F5E9; border-left: 4px solid #4CAF50; padding: 15px; border-radius: 4px;'>
                            <p style='margin: 0; font-size: 14px;'><strong>✓ Pedido guardado en la base de datos</strong></p>
                            <p style='margin: 5px 0 0 0; font-size: 13px;'>ID del pedido: <strong>{order.Id}</strong></p>
                        </div>
                    </div>
                </div>
            ";

            // Enviar email al CLIENTE
            await _emailService.SendEmailAsync(
                toEmail: request.CustomerEmail,
                subject: clientSubject,
                htmlBody: clientBody
            );

            // Enviar email a la EMPRESA
            await _emailService.SendEmailAsync(
                toEmail: ownerEmail,
                subject: adminSubject,
                htmlBody: adminBody
            );

            _logger.LogInformation("✅ Emails enviados correctamente (Cliente + Empresa)");

            return Ok(new { message = "Pedido recibido correctamente", success = true, orderNumber = order.Id.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al procesar pedido");
            return StatusCode(500, new { message = "Error al procesar el pedido", error = ex.Message });
        }
    }

    // Métodos auxiliares para extraer ciudad y código postal
    private string ExtractCity(string address)
    {
        try
        {
            var parts = address.Split(',');
            return parts.Length > 1 ? parts[^2].Trim() : "N/A";
        }
        catch
        {
            return "N/A";
        }
    }

    private string ExtractPostalCode(string address)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(address, @"\b\d{5}\b");
            return match.Success ? match.Value : "N/A";
        }
        catch
        {
            return "N/A";
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
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Color { get; set; }
    public string? Length { get; set; }
}
public class CreateOrderRequest
{
    public int? UserId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }
}

public class UpdateOrderStatusRequest
{
    public int Status { get; set; }
}

