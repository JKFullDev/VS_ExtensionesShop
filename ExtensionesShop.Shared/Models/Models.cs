namespace ExtensionesShop.Shared.Models;

/// <summary>
/// Producto del catálogo de extensiones.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }

    public string? Color { get; set; }
    public decimal? Centimeters { get; set; }
}

/// <summary>
/// Categoría de productos.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Subcategory> Subcategories { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}

/// <summary>
/// Subcategoría de productos.
/// </summary>
public class Subcategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public List<Product> Products { get; set; } = new();
}

/// <summary>
/// Elemento del carrito de compras (en sesión / localStorage → API).
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}

/// <summary>
/// DTO del carrito completo.
/// </summary>
public class CartSummary
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public decimal ShippingCost { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? DiscountCode { get; set; }
    public decimal Total => Subtotal + ShippingCost - (DiscountAmount ?? 0);
    public int TotalItems => Items.Sum(i => i.Quantity);
}

/// <summary>
/// Usuario del sistema.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Order> Orders { get; set; } = new();
}

/// <summary>
/// Pedido realizado por un cliente.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Datos del cliente (se guardan incluso si es invitado)
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    // Totales
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    // Estado y fechas
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Notas
    public string? Notes { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Línea de pedido.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}

/// <summary>
/// Estado del pedido.
/// </summary>
public enum OrderStatus
{
    Pending = 0,      // Pendiente de pago
    Confirmed = 1,    // Confirmado
    Processing = 2,   // En preparación
    Shipped = 3,      // Enviado
    Delivered = 4,    // Entregado
    Cancelled = 5     // Cancelado
}

/// <summary>
/// DTO para crear un pedido.
/// </summary>
public class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
