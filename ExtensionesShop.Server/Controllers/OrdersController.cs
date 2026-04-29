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
    private readonly IConfiguration _configuration;

    public OrdersController(AppDbContext db, IEmailService emailService, ILogger<OrdersController> logger, IConfiguration configuration)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
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
            .ThenInclude(p => p.Images)
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
            .ThenInclude(p => p.Images)
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

                    if (variant != null)
                    {
                        variant.Stock -= item.Quantity;
                    }
                    else
                    {
                        // Si no encuentra una variante exacta, buscar la primera disponible
                        variant = product.Variants.FirstOrDefault();
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
                    Subtotal = order.Subtotal,  // ✅ Incluir Subtotal
                    ShippingCost = order.ShippingCost,  // ✅ Incluir Costo de Envío
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
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // 🔄 LÓGICA DE RESTOCK AL CANCELAR
            // Status 5 = Cancelado
            if (request.Status == 5 && order.Status != 5)
            {
                _logger.LogInformation("♻️ Iniciando restock para pedido #{OrderId}", id);

                // Cargar productos y variantes para actualizar stock
                var productIds = order.OrderItems
                    .Select(oi => oi.ProductId)
                    .Where(pid => pid.HasValue)
                    .Select(pid => pid.Value)
                    .Distinct()
                    .ToList();

                var products = await _db.Products
                    .AsTracking()
                    .Include(p => p.Variants)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                var allVariantIds = products
                    .SelectMany(p => p.Variants.Select(v => v.Id))
                    .Distinct()
                    .ToList();

                var variants = allVariantIds.Any()
                    ? await _db.ProductVariants.AsTracking().Where(v => allVariantIds.Contains(v.Id)).ToListAsync()
                    : new List<ProductVariant>();

                // Procesar cada item del pedido
                foreach (var item in order.OrderItems)
                {
                    if (!item.ProductId.HasValue)
                        continue;

                    var product = products.FirstOrDefault(p => p.Id == item.ProductId.Value);
                    if (product == null)
                        continue;

                    // Si el item tiene color y centimeters, buscar la variante correspondiente
                    if (!string.IsNullOrEmpty(item.SelectedColor) || item.SelectedCentimeters.HasValue)
                    {
                        var variant = product.Variants.FirstOrDefault(v =>
                            (v.Color ?? string.Empty).Trim() == (item.SelectedColor ?? string.Empty).Trim() &&
                            Math.Abs((v.Centimeters ?? 0) - (item.SelectedCentimeters ?? 0)) < 0.01m);

                        if (variant != null)
                        {
                            variant.Stock += item.Quantity;
                            _logger.LogInformation("✅ Restock: {ProductName} ({Color}, {Cm}cm) +{Qty} → {NewStock}", 
                                item.ProductName, variant.Color, variant.Centimeters, item.Quantity, variant.Stock);
                        }
                    }
                    // Si no tiene variantes, devolver al stock general
                    else if (!product.Variants.Any())
                    {
                        product.StockValue += item.Quantity;
                        _logger.LogInformation("✅ Restock: {ProductName} +{Qty} → {NewStock}", 
                            item.ProductName, item.Quantity, product.StockValue);
                    }
                }
            }

            // Actualizar estado y timestamps
            order.Status = request.Status;

            if (request.Status == 3) // Shipped
                order.ShippedAt = DateTime.UtcNow;
            else if (request.Status == 4) // Delivered
                order.DeliveredAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("✅ Pedido #{OrderId} actualizado a estado {Status}", id, request.Status);
            return Ok(order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Error actualizando estado del pedido #{OrderId}", id);
            return StatusCode(500, new { message = "Error al actualizar el estado del pedido" });
        }
    }

    /// <summary>
    /// POST /api/orders/place-order - Crear pedido con validación de stock y transacción
    /// Valida stock, crea orden, resta stock, vacía carrito y notifica al admin
    /// </summary>
    [HttpPost("place-order")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("📦 Iniciando creación de pedido para: {Email}", request.CustomerEmail);

            // ========================================
            // 1. VALIDAR DATOS BÁSICOS
            // ========================================
            if (string.IsNullOrEmpty(request.CustomerEmail) ||
                string.IsNullOrEmpty(request.CustomerName) ||
                request.Items == null || !request.Items.Any())
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Datos del pedido incompletos" });
            }

            // ========================================
            // 3. CARGAR TODOS LOS PRODUCTOS Y VARIANTES (rastreados)
            // ========================================
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var variantIds = request.Items.Where(i => i.ProductVariantId.HasValue).Select(i => i.ProductVariantId.Value).Distinct().ToList();

            var products = await _db.Products
                .AsTracking() // ✅ Asegurar que EF Core los rastrée
                .Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var variants = variantIds.Any()
                ? await _db.ProductVariants.AsTracking().Where(v => variantIds.Contains(v.Id)).ToListAsync()
                : new List<ProductVariant>();

            // ========================================
            // 4. OBTENER USER ID SI ESTÁ AUTENTICADO
            // ========================================
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

            // ========================================
            // 5. CALCULAR COSTO DE ENVÍO
            // ========================================
            decimal subtotal = request.Subtotal;  // ✅ Usar Subtotal enviado desde cliente
            decimal shippingCost = subtotal >= 120 ? 0 : 7;  // Envío gratis si >= 120€, sino 7€
            decimal totalFinal = request.Total;  // ✅ Usar Total que ya viene calculado

            _logger.LogInformation("💰 Cálculo de envío: Subtotal={Subtotal}€, Envío={Envío}€, Total={Total}€", subtotal, shippingCost, totalFinal);

            // ========================================
            // 6. CREAR ORDEN Y RESTAR STOCK
            // ========================================
            var order = new Order
            {
                UserId = userId,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress,
                City = request.City,
                Province = request.Province,
                PostalCode = request.PostalCode,
                Subtotal = subtotal,
                ShippingCost = shippingCost,  // ✅ Asignar costo de envío calculado
                Total = totalFinal,  // ✅ Total con envío incluido
                Status = 0, // Pendiente de Pago
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // Agregar items y restar stock
            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);

                // Crear OrderItem
                order.OrderItems.Add(new ExtensionesShop.Shared.Models.OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    SelectedColor = item.SelectedColor,
                    SelectedCentimeters = item.SelectedCentimeters
                });

                // ========================================
                // RESTAR STOCK
                // ========================================
                // Si item tiene ProductVariantId, restar de esa variante específica
                if (item.ProductVariantId.HasValue)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId.Value);
                    if (variant != null)
                    {
                        variant.Stock -= item.Quantity;
                        _logger.LogInformation("📉 Stock reducido para variante {VariantId} de {ProductName}: {NewStock}", 
                            variant.Id, product.Name, variant.Stock);
                    }
                }
                // Si el producto tiene variantes, restar de la variante que coincida
                else if (product.Variants.Any())
                {
                    var variant = product.Variants.FirstOrDefault(v =>
                        (v.Color ?? string.Empty).Trim() == (item.SelectedColor ?? string.Empty).Trim() &&
                        Math.Abs((v.Centimeters ?? 0) - (item.SelectedCentimeters ?? 0)) < 0.01m);

                    if (variant != null)
                    {
                        variant.Stock -= item.Quantity;
                        _logger.LogInformation("📉 Stock reducido para {ProductName} ({Color}, {Cm}cm): {NewStock}", 
                            product.Name, variant.Color, variant.Centimeters, variant.Stock);
                    }
                }
                // Si no tiene variantes, restar del stock general
                else
                {
                    product.StockValue -= item.Quantity;
                    _logger.LogInformation("📉 Stock reducido para {ProductName}: {NewStock}", product.Name, product.StockValue);
                }
            }

            // Guardar orden y cambios de stock
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Pedido #{OrderId} creado y stock reducido", order.Id);

            // ========================================
            // 7. VACIAR CARRITO DEL USUARIO
            // ========================================
            if (userId.HasValue)
            {
                var cartItems = await _db.CartItems
                    .Where(ci => ci.UserId == userId.Value)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    _db.CartItems.RemoveRange(cartItems);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("🗑️ Carrito vaciado para usuario {UserId}", userId);
                }
            }

            // ========================================
            // 8. NOTIFICAR AL ADMINISTRADOR
            // ========================================
            try
            {
                var adminEmail = _configuration["Email:OwnerEmail"] ?? "hola@extensiones.shop";
                var adminBody = GenerateAdminOrderEmailHtml(order, request, products);

                await _emailService.SendEmailAsync(
                    toEmail: adminEmail,
                    subject: $"🛍️ NUEVO PEDIDO #{order.Id} - {order.CustomerName}",
                    htmlBody: adminBody
                );

                _logger.LogInformation("📧 Email de notificación enviado al administrador");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error al enviar email al administrador (no afecta al pedido)");
                // No fallar si falla el email
            }

            // ========================================
            // 9. ENVIAR CONFIRMACIÓN AL CLIENTE
            // ========================================
            try
            {
                var clientBody = GenerateClientOrderEmailHtml(order, request, products);

                await _emailService.SendEmailAsync(
                    toEmail: request.CustomerEmail,
                    subject: $"✅ Pedido Confirmado #{order.Id}",
                    htmlBody: clientBody
                );

                _logger.LogInformation("📧 Email de confirmación enviado al cliente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error al enviar email al cliente (no afecta al pedido)");
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Pedido creado correctamente",
                success = true,
                orderNumber = order.Id.ToString()
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Error al procesar pedido. Transacción revertida");
            return StatusCode(500, new { message = "Error al procesar el pedido. Por favor, inténtalo de nuevo.", error = ex.Message });
        }
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
                Province = request.ShippingAddress, // Se puede mejorar si se envía explícitamente
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
                var details = "";
                if (!string.IsNullOrEmpty(item.Color)) details += $" - {item.Color}";
                if (!string.IsNullOrEmpty(item.Length)) details += $" - {item.Length}cm";

                adminBody += $@"
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 12px;'>{item.ProductName}{details}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Color ?? "-"}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Length ?? "-"}</td>
                            <td style='padding: 12px; text-align: center;'>{item.Quantity}</td>
                            <td style='padding: 12px; text-align: right;'>€{item.UnitPrice:N2}</td>
                            <td style='padding: 12px; text-align: right; font-weight: bold;'>€{item.Subtotal:N2}</td>
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

    /// <summary>
    /// Genera HTML profesional para email al administrador
    /// </summary>
    private string GenerateAdminOrderEmailHtml(Order order, PlaceOrderRequest request, List<Product> products)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; color: #333; line-height: 1.6; }");
        sb.AppendLine(".container { max-width: 700px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #E8607A 0%, #d94a65 100%); color: white; padding: 30px 20px; text-align: center; border-radius: 10px 10px 0 0; }");
        sb.AppendLine(".header h1 { margin: 0; font-size: 28px; }");
        sb.AppendLine(".header p { margin: 5px 0 0 0; opacity: 0.9; }");
        sb.AppendLine(".content { background: #fff; padding: 30px; border: 1px solid #ddd; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { color: #E8607A; font-size: 18px; font-weight: 700; margin-bottom: 15px; border-bottom: 2px solid #E8607A; padding-bottom: 8px; }");
        sb.AppendLine(".info-row { padding: 10px 0; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine(".info-row:last-child { border-bottom: none; }");
        sb.AppendLine(".info-label { font-weight: 700; color: #555; display: inline-block; width: 140px; }");
        sb.AppendLine(".info-value { color: #333; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        sb.AppendLine("th { background: #F0E8EB; padding: 12px; text-align: left; font-weight: 700; color: #555; border-bottom: 2px solid #E8607A; }");
        sb.AppendLine("td { padding: 12px; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine("td:last-child { text-align: right; }");
        sb.AppendLine(".total-row { background: #FDF0F3; font-weight: 700; color: #E8607A; font-size: 16px; }");
        sb.AppendLine(".action-needed { background: #FFF3CD; border-left: 4px solid #FFC107; padding: 15px; border-radius: 4px; margin-top: 20px; }");
        sb.AppendLine(".action-needed h4 { margin-top: 0; color: #856404; }");
        sb.AppendLine(".footer { text-align: center; padding: 20px; color: #999; font-size: 12px; border-top: 1px solid #ddd; margin-top: 20px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='container'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>✦ NUEVO PEDIDO</h1>");
        sb.AppendLine($"<p>Pedido #{order.Id} - {order.CreatedAt:dd/MM/yyyy HH:mm:ss}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='content'>");

        // Datos del Cliente
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>👤 DATOS DEL CLIENTE</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Nombre:</span><span class='info-value'><strong>{order.CustomerName}</strong></span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Email:</span><span class='info-value'><a href='mailto:{order.CustomerEmail}'>{order.CustomerEmail}</a></span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Teléfono:</span><span class='info-value'><a href='tel:{order.CustomerPhone}'>{order.CustomerPhone}</a></span></div>");
        sb.AppendLine("</div>");

        // Dirección de Envío
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>📦 DIRECCIÓN DE ENVÍO</div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Dirección:</span><span class='info-value'>{order.ShippingAddress}</span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Ciudad:</span><span class='info-value'>{order.City}</span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Provincia:</span><span class='info-value'>{order.Province}</span></div>");
        sb.AppendLine($"<div class='info-row'><span class='info-label'>Código Postal:</span><span class='info-value'>{order.PostalCode}</span></div>");
        sb.AppendLine("</div>");

        // Items del Pedido
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>🛍️ ARTÍCULOS DEL PEDIDO</div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th style='width: 80px;'>Imagen</th><th>Producto</th><th>Cantidad</th><th>Precio Unit.</th><th>Subtotal</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var item in request.Items)
        {
            // Construir descripción completa del producto con variantes
            var detalles = new List<string>();

            // Agregar color si existe (de la variante o del producto padre)
            var colorDisplay = item.SelectedColor;
            if (string.IsNullOrEmpty(colorDisplay))
            {
                var producto = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (producto != null && !string.IsNullOrEmpty(producto.Color))
                {
                    colorDisplay = producto.Color;
                }
            }

            if (!string.IsNullOrEmpty(colorDisplay))
            {
                detalles.Add(colorDisplay);
            }

            // Agregar centímetros si existen (de la variante o del producto padre)
            decimal? cmDisplay = item.SelectedCentimeters;
            if (!cmDisplay.HasValue || cmDisplay == 0)
            {
                var producto = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (producto != null && producto.Centimeters.HasValue && producto.Centimeters > 0)
                {
                    cmDisplay = producto.Centimeters;
                }
            }

            if (cmDisplay.HasValue && cmDisplay > 0)
            {
                detalles.Add($"{cmDisplay}cm");
            }

            // Combinar el nombre del producto con los detalles
            var descripcionCompleta = detalles.Any()
                ? $"{item.ProductName} - {string.Join(" / ", detalles)}"
                : item.ProductName;

            // Construir HTML de la fila con imagen
            sb.AppendLine($"<tr>");

            // Columna de imagen
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                sb.AppendLine($"<td style='text-align: center;'><img src='{item.ImageUrl}' alt='{item.ProductName}' style='width: 60px; height: 60px; object-fit: cover; border-radius: 6px;' /></td>");
            }
            else
            {
                sb.AppendLine($"<td style='text-align: center;'><div style='width: 60px; height: 60px; background: #f0f0f0; border-radius: 6px; display: flex; align-items: center; justify-content: center; font-size: 12px; color: #999;'>Sin imagen</div></td>");
            }

            sb.AppendLine($"<td><strong>{descripcionCompleta}</strong></td>");
            sb.AppendLine($"<td style='text-align: center;'>{item.Quantity}</td>");
            sb.AppendLine($"<td>{item.UnitPrice:N2}€</td>");
            sb.AppendLine($"<td>{(item.UnitPrice * item.Quantity):N2}€</td>");
            sb.AppendLine($"</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        // ✅ Desglose de Costos para Admin
        sb.AppendLine("<div style='margin-top: 20px; padding: 15px; background: #fff3f5; border-radius: 8px; border-left: 4px solid #E8607A;'>");
        sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0;'>");
        sb.AppendLine($"<span><strong>Subtotal:</strong></span>");
        sb.AppendLine($"<span>{order.Subtotal:N2}€</span>");
        sb.AppendLine("</div>");

        if (order.ShippingCost > 0)
        {
            sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0; border-top: 1px solid #ffc0cb;'>");
            sb.AppendLine($"<span><strong>Envío:</strong></span>");
            sb.AppendLine($"<span>{order.ShippingCost:N2}€</span>");
            sb.AppendLine("</div>");
        }
        else
        {
            sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0; border-top: 1px solid #ffc0cb;'>");
            sb.AppendLine($"<span><strong>Envío:</strong></span>");
            sb.AppendLine($"<span style='color: #059669; font-weight: bold;'>GRATIS ✓</span>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 12px 0; border-top: 2px solid #E8607A; font-size: 18px; font-weight: bold; color: #E8607A;'>");
        sb.AppendLine($"<span>TOTAL:</span>");
        sb.AppendLine($"<span>{order.Total:N2}€</span>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Notas si existen
        if (!string.IsNullOrEmpty(request.Notes))
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>📝 NOTAS DEL CLIENTE</div>");
            sb.AppendLine($"<p>{request.Notes}</p>");
            sb.AppendLine("</div>");
        }

        // Action Required
        // Action Required (Email para Vero)
        sb.AppendLine("<div class='action-needed'");
        sb.AppendLine("<h4 style='color: #E65100; margin-top: 0;'>⚠️ NUEVO PEDIDO PENDIENTE</h4>");
        sb.AppendLine($"<p>El cliente <strong>{order.CustomerName}</strong> ha registrado un pedido y ya tiene tus datos de pago.</p>");
        sb.AppendLine("<p><strong>¿Qué debes hacer ahora?</strong></p>");
        sb.AppendLine("<ul style='line-height: 1.6;'>");
        sb.AppendLine("<li><strong>Revisa tu Bizum o Banco:</strong> Comprueba si ha llegado el pago con el importe total.</li>");
        sb.AppendLine("<li><strong>Confirma al cliente:</strong> Si te escribe por WhatsApp, confírmale que el pago es correcto.</li>");
        sb.AppendLine("<li><strong>Prepara el paquete:</strong> Tienes 24-48h para realizar el envío.</li>");
        sb.AppendLine("<li><strong>Gestión:</strong> Entra al panel de administración para marcarlo como 'Pagado' o 'Enviado' cuando lo saques.</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p style='font-size: 13px; font-style: italic; color: #666;'>Recuerda que hasta que no verifiques el ingreso, no debes enviar el producto.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>Este es un correo automático. Por favor, no respondas directamente a este mensaje.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Genera HTML para email de confirmación al cliente
    /// </summary>
    private string GenerateClientOrderEmailHtml(Order order, PlaceOrderRequest request, List<Product> products)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; color: #333; line-height: 1.6; }");
        sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #E8607A 0%, #d94a65 100%); color: white; padding: 30px 20px; text-align: center; border-radius: 10px 10px 0 0; }");
        sb.AppendLine(".header h1 { margin: 0; font-size: 24px; }");
        sb.AppendLine(".content { background: #fff; padding: 30px; border: 1px solid #ddd; }");
        sb.AppendLine(".message { background: #F0E8EB; padding: 20px; border-radius: 8px; margin-bottom: 25px; border-left: 4px solid #E8607A; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { color: #E8607A; font-size: 16px; font-weight: 700; margin-bottom: 15px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        sb.AppendLine("th { background: #F0E8EB; padding: 12px; text-align: left; font-weight: 700; }");
        sb.AppendLine("td { padding: 12px; border-bottom: 1px solid #f0f0f0; }");
        sb.AppendLine("td:last-child { text-align: right; }");
        sb.AppendLine(".total { background: #FDF0F3; padding: 15px; border-radius: 8px; font-size: 18px; font-weight: 700; color: #E8607A; text-align: right; margin-top: 20px; }");
        sb.AppendLine(".footer { text-align: center; padding: 20px; color: #999; font-size: 12px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='container'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>✅ ¡Pedido Confirmado!</h1>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='content'>");

        // Mensaje Principal
        sb.AppendLine("<div class='message'>");
        sb.AppendLine($"<p>¡Hola {order.CustomerName.Split(' ')[0]}! 👋</p>");
        sb.AppendLine($"<p>Hemos recibido tu pedido <strong>#{order.Id}</strong> correctamente.</p>");
        sb.AppendLine("<p><strong>Nos pondremos en contacto contigo en las próximas horas para coordinar el pago y confirmar el envío.</strong></p>");
        sb.AppendLine("</div>");

        // Detalles del Pedido
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>📦 Resumen de tu Pedido</div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th style='width: 80px;'>Imagen</th><th>Producto</th><th>Cantidad</th><th>Subtotal</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var item in request.Items)
        {
            // Construir descripción completa del producto con variantes
            var detalles = new List<string>();

            // Agregar color si existe (de la variante o del producto padre)
            var colorDisplay = item.SelectedColor;
            if (string.IsNullOrEmpty(colorDisplay))
            {
                var producto = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (producto != null && !string.IsNullOrEmpty(producto.Color))
                {
                    colorDisplay = producto.Color;
                }
            }

            if (!string.IsNullOrEmpty(colorDisplay))
            {
                detalles.Add(colorDisplay);
            }

            // Agregar centímetros si existen (de la variante o del producto padre)
            decimal? cmDisplay = item.SelectedCentimeters;
            if (!cmDisplay.HasValue || cmDisplay == 0)
            {
                var producto = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (producto != null && producto.Centimeters.HasValue && producto.Centimeters > 0)
                {
                    cmDisplay = producto.Centimeters;
                }
            }

            if (cmDisplay.HasValue && cmDisplay > 0)
            {
                detalles.Add($"{cmDisplay}cm");
            }

            // Combinar el nombre del producto con los detalles
            var descripcionCompleta = detalles.Any()
                ? $"{item.ProductName} - {string.Join(" / ", detalles)}"
                : item.ProductName;

            sb.AppendLine($"<tr>");

            // Columna de imagen
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                sb.AppendLine($"<td style='text-align: center;'><img src='{item.ImageUrl}' alt='{item.ProductName}' style='width: 60px; height: 60px; object-fit: cover; border-radius: 6px;' /></td>");
            }
            else
            {
                sb.AppendLine($"<td style='text-align: center;'><div style='width: 60px; height: 60px; background: #f0f0f0; border-radius: 6px; display: flex; align-items: center; justify-content: center; font-size: 12px; color: #999;'>Sin imagen</div></td>");
            }

            sb.AppendLine($"<td><strong>{descripcionCompleta}</strong></td>");
            sb.AppendLine($"<td style='text-align: center;'>{item.Quantity}</td>");
            sb.AppendLine($"<td>{(item.UnitPrice * item.Quantity):N2}€</td>");
            sb.AppendLine($"</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        // ✅ Desglose de Costos
        sb.AppendLine("<div style='margin-top: 20px; padding: 15px; background: #f9f9f9; border-radius: 8px;'>");
        sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #ddd;'>");
        sb.AppendLine($"<span><strong>Subtotal:</strong></span>");
        sb.AppendLine($"<span>{order.Subtotal:N2}€</span>");
        sb.AppendLine("</div>");

        if (order.ShippingCost > 0)
        {
            sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #ddd;'>");
            sb.AppendLine($"<span><strong>Envío:</strong></span>");
            sb.AppendLine($"<span>{order.ShippingCost:N2}€</span>");
            sb.AppendLine("</div>");
        }
        else
        {
            sb.AppendLine($"<div style='display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #ddd;'>");
            sb.AppendLine($"<span><strong>Envío:</strong></span>");
            sb.AppendLine($"<span style='color: #059669; font-weight: bold;'>GRATIS ✓</span>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");

        sb.AppendLine($"<div class='total'>TOTAL: {order.Total:N2}€</div>");
        sb.AppendLine("</div>");

        // Próximos Pasos
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>📋 Próximos Pasos</div>");
        sb.AppendLine("<ol style='line-height: 1.8;'>");
        sb.AppendLine("<li>Realiza el pago a través de una de estas opciones indicando tu nombre y el número de pedido: <br />" +
                                                            "   - Bizum a este número de teléfono: <strong>657 557 051</strong>.<br />" +
                                                            "   - Transferencia a este número de cuenta:<strong>ES11 1111 1111 1111</strong>.</li>");
        sb.AppendLine("<li>Envía el comprobante por WhatsApp (<strong>657 557 051</strong>) o por correo electrónico (<strong>info@extensiones.shop</strong>) para confirmar tu pedido.</li>");
        sb.AppendLine("<li>Una vez confirmado el pago, prepararemos y enviaremos tu pedido en un plazo de 24-48 horas.</li>");
        sb.AppendLine("<li>En cuanto el paquete salga, recibirás tu número de tracking para localizarlo.</li>");
        sb.AppendLine("</ol>");
        sb.AppendLine("</div>");

        // Contact Info
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>📞 ¿Preguntas?</div>");
        sb.AppendLine("<p>Si tienes alguna pregunta, no dudes en responder a este correo o contactarnos a través de nuestro sitio web.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>Gracias por tu compra. ¡Esperamos verte pronto! 💖</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
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

/// <summary>
/// DTO para crear un pedido con validación de stock y transacción
/// </summary>
public class PlaceOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<PlaceOrderItemRequest> Items { get; set; } = new();
    public decimal Subtotal { get; set; }  // ✅ Subtotal sin envío
    public decimal Total { get; set; }      // ✅ Total con envío
}

public class PlaceOrderItemRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }  // ✅ NUEVO: ID de variante específica
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }
    public string? ImageUrl { get; set; }  // ✅ NUEVO: URL de la imagen del producto
}

