using ExtensionesShop.Server.Data;
using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
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
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? NotFound() : Ok(order);
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
                var product = await _db.Products.FindAsync(item.ProductId);
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

                // Reducir stock
                product.Stock -= item.Quantity;
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
}

// DTOs
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

