using ExtensionesShop.Server.Data;
using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(AppDbContext db, IEmailService emailService) : ControllerBase
{
    // GET api/orders (admin o usuario logueado)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll([FromQuery] int? userId)
    {
        var query = db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    // GET api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? NotFound() : Ok(order);
    }

    // POST api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.Items.Any())
            return BadRequest(new { message = "El pedido debe contener al menos un producto" });

        // Calcular totales
        var subtotal = request.Items.Sum(i => i.Subtotal);
        var shippingCost = 5.00m; // Puedes calcularlo dinámicamente
        var total = subtotal + shippingCost;

        // Crear la orden
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            PostalCode = request.PostalCode,
            Notes = request.Notes,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            Total = total,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Agregar items
        foreach (var cartItem in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                UnitPrice = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
                SelectedColor = cartItem.SelectedColor,
                SelectedCentimeters = cartItem.SelectedCentimeters
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Cargar las relaciones para el email
        await db.Entry(order).Collection(o => o.Items).LoadAsync();

        // Enviar emails
        try
        {
            await emailService.SendOrderConfirmationToCustomerAsync(order);
            await emailService.SendOrderNotificationToCompanyAsync(order);
        }
        catch (Exception ex)
        {
            // Log el error pero no fallar la creación del pedido
            Console.WriteLine($"Error sending emails: {ex.Message}");
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // PUT api/orders/{id}/status
    [HttpPut("{id}/status")]
    public async Task<ActionResult<Order>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await db.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = request.Status;

        if (request.Status == OrderStatus.Shipped && !order.ShippedAt.HasValue)
            order.ShippedAt = DateTime.UtcNow;

        if (request.Status == OrderStatus.Delivered && !order.DeliveredAt.HasValue)
            order.DeliveredAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(order);
    }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
