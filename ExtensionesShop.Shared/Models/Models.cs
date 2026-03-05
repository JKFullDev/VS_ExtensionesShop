namespace ExtensionesShop.Shared.Models;

/// <summary>
/// Producto base del catálogo de extensiones.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public int Stock { get; set; }

    // Atributos específicos de extensiones
    public string? HairType { get; set; }       // Remy, Virgin, Synthetic
    public string? Length { get; set; }         // 40cm, 50cm, 60cm...
    public string? Weight { get; set; }         // 100g, 120g...
    public string? Color { get; set; }
    public string? ApplicationMethod { get; set; } // Clip-In, Tape-In, Keratin

    public decimal? DiscountPercentage =>
        OriginalPrice.HasValue && OriginalPrice > 0
            ? Math.Round((1 - Price / OriginalPrice.Value) * 100, 0)
            : null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Categoría de productos.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
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
    public string? SelectedLength { get; set; }

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
