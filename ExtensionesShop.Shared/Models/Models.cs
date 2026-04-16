namespace ExtensionesShop.Shared.Models;

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
/// Producto base del catálogo de extensiones.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Stock de la base de datos (para productos sin variantes).
    /// Para productos con variantes, se calcula desde las variantes.
    /// </summary>
    public int StockValue { get; set; }

    /// <summary>
    /// Stock calculado inteligentemente:
    /// - Si hay variantes: suma del stock de todas las variantes
    /// - Si no hay variantes: usa el valor de StockValue
    /// </summary>
    public int Stock 
    { 
        get => (Variants != null && Variants.Any())
            ? Variants.Sum(v => v.Stock)
            : StockValue;
    }

    /// <summary>
    /// Precio mínimo: el menor precio entre variantes, o el precio base si no hay variantes.
    /// Se usa para mostrar "Desde $80" en el catálogo.
    /// </summary>
    public decimal PrecioMinimo
    {
        get => (Variants != null && Variants.Any()) 
            ? Variants.Min(v => v.Price) 
            : Price;
    }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }

    public string? Color { get; set; }
    public decimal? Centimeters { get; set; }

    /// <summary>
    /// Indica si el producto está activo (visible) o ha sido eliminado lógicamente.
    /// ✅ Soft Delete: Los datos nunca se pierden, solo se marcan como inactivos.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties para variantes e imágenes
    public List<ProductVariant> Variants { get; set; } = new();
    public List<ProductImage> Images { get; set; } = new();
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
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Rol del usuario (Admin, User)
    public string Role { get; set; } = "User";

    // Verificación de email
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // Recuperación de contraseña
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}

/// <summary>
/// Pedido (Orden).
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Datos de envío (Snapshot)
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

    // Estado: 0=Pending, 1=Confirmed, 2=Processing, 3=Shipped, 4=Delivered, 5=Cancelled
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}

/// <summary>
/// Línea de pedido (detalle).
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }  // ✅ Ahora es nullable
    public Product? Product { get; set; }

    // Datos persistidos para histórico
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }
}

/// <summary>
/// Elemento del carrito de compras (en sesión / localStorage → API).
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public decimal? SelectedCentimeters { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}

/// <summary>
/// Respuesta de item del carrito desde la API (incluye datos de variante).
/// </summary>
public class CartItemResponse
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }  // Precio de la variante o producto
    public int Quantity { get; set; }
    public string? VariantColor { get; set; }  // ✅ NUEVO
    public decimal? VariantCentimeters { get; set; }  // ✅ NUEVO

    public decimal Subtotal => UnitPrice * Quantity;
}

/// <summary>
/// Item del carrito para el cliente (compatible con vista actual).
/// </summary>
public class CarritoItemView
{
    public Product Producto { get; set; } = new();
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }  // ✅ NUEVO: Precio en el momento de la compra
    public int? VariantId { get; set; }  // ✅ NUEVO: ID de variante si existe
    public string? VariantColor { get; set; }  // ✅ NUEVO
    public decimal? VariantCentimeters { get; set; }  // ✅ NUEVO

    public decimal Subtotal => PrecioUnitario * Cantidad;
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

// =============================================
// MODELOS PARA BACKEND (BD)
// =============================================

/// <summary>
/// Favorito de un usuario (Backend - BD).
/// </summary>
public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Product? Product { get; set; }
}

/// <summary>
/// Item del carrito de un usuario (Backend - BD).
/// Ahora guarda explícitamente los datos de la variante seleccionada.
/// </summary>
public class CartItemEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }

    // ✅ NUEVO: Guardar precio de la variante en el momento de la compra
    public decimal UnitPrice { get; set; }

    // ✅ NUEVO: Guardar detalles de la variante para historial
    public string? VariantColor { get; set; }
    public decimal? VariantCentimeters { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Product? Product { get; set; }
}

// =============================================
// DTOs PARA API
// =============================================

/// <summary>
/// DTO para sincronizar carrito local con BD.
/// </summary>
public class SyncCartRequest
{
    public List<CartItemSync> Items { get; set; } = new();
}

/// <summary>
/// Item del carrito para sincronización.
/// </summary>
public class CartItemSync
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }  // ✅ NUEVO: Identificar variante específica
    public int Quantity { get; set; }
}

/// <summary>
/// DTO para agregar/actualizar item en carrito.
/// </summary>
public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Respuesta genérica de operaciones.
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// =============================================
// MODELOS PARA VARIANTES E IMÁGENES DE PRODUCTOS
// =============================================

/// <summary>
/// Variante de un producto (Color, Talla/Longitud, Stock, Precio).
/// Permite gestionar múltiples combinaciones de atributos para un mismo producto base.
/// </summary>
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    // Atributos de la variante
    public string? Color { get; set; }
    public decimal? Centimeters { get; set; }

    // Inventario
    public int Stock { get; set; }

    // Precio puede variar por variante
    public decimal Price { get; set; }

    // Orden de visualización
    public int DisplayOrder { get; set; }

    // Control
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product? Product { get; set; }
    public List<ProductImage> Images { get; set; } = new();
}

/// <summary>
/// Imagen de un producto o variante.
/// Permite gestionar múltiples imágenes por producto o por variante específica.
/// </summary>
public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    // Información de la imagen
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }

    // Control
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product? Product { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}

/// <summary>
/// DTO para crear/actualizar productos con sus variantes e imágenes.
/// </summary>
public class CreateProductRequest
{
    public int? Id { get; set; } // NULL para nuevo, valor para editar
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }

    public int CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
    public string? Color { get; set; }
    public decimal? Centimeters { get; set; }

    // Variantes
    public List<ProductVariantDto> Variants { get; set; } = new();

    // Imágenes
    public List<ProductImageDto> Images { get; set; } = new();
}

/// <summary>
/// DTO para variantes.
/// </summary>
public class ProductVariantDto
{
    public int? Id { get; set; }
    public string? Color { get; set; }
    public decimal? Centimeters { get; set; }
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO para imágenes.
/// </summary>
public class ProductImageDto
{
    public int? Id { get; set; }
    public int? ProductVariantId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
