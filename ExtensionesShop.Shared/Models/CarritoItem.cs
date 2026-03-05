namespace ExtensionesShop.Shared.Models;

/// <summary>
/// Representa un item en el carrito de compras.
/// </summary>
public class CarritoItem
{
    public Product Producto { get; set; } = null!;
    public int Cantidad { get; set; } = 1;

    public decimal Subtotal => Producto.Price * Cantidad;

    public bool EsValido => Producto != null && Cantidad > 0;
}