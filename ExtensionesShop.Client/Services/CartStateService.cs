using ExtensionesShop.Shared.Models;

namespace ExtensionesShop.Client.Services;

public class CartStateService
{
    private readonly List<CarritoItem> _items = new();

    // Evento para notificar cambios
    public event Action? OnChange;

    // Propiedades públicas
    public IReadOnlyList<CarritoItem> Items => _items.AsReadOnly();
    public int CantidadTotal => _items.Sum(i => i.Cantidad);
    public decimal Total => _items.Sum(i => i.Subtotal);

    /// <summary>
    /// Añade un producto al carrito o incrementa su cantidad si ya existe.
    /// </summary>
    public void AgregarProducto(Product producto, int cantidad = 1)
    {
        if (producto == null || cantidad <= 0) return;

        var itemExistente = _items.FirstOrDefault(i => i.Producto.Id == producto.Id);

        if (itemExistente != null)
        {
            itemExistente.Cantidad += cantidad;
        }
        else
        {
            _items.Add(new CarritoItem
            {
                Producto = producto,
                Cantidad = cantidad
            });
        }

        NotificarCambios();
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito.
    /// </summary>
    public void ActualizarCantidad(int productoId, int nuevaCantidad)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item == null) return;

        if (nuevaCantidad <= 0)
        {
            EliminarProducto(productoId);
        }
        else
        {
            item.Cantidad = nuevaCantidad;
            NotificarCambios();
        }
    }

    /// <summary>
    /// Incrementa la cantidad de un producto.
    /// </summary>
    public void IncrementarCantidad(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null)
        {
            item.Cantidad++;
            NotificarCambios();
        }
    }

    /// <summary>
    /// Decrementa la cantidad de un producto.
    /// </summary>
    public void DecrementarCantidad(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null)
        {
            if (item.Cantidad > 1)
            {
                item.Cantidad--;
                NotificarCambios();
            }
            else
            {
                EliminarProducto(productoId);
            }
        }
    }

    /// <summary>
    /// Elimina un producto del carrito.
    /// </summary>
    public void EliminarProducto(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null)
        {
            _items.Remove(item);
            NotificarCambios();
        }
    }

    /// <summary>
    /// Vacía el carrito completamente.
    /// </summary>
    public void VaciarCarrito()
    {
        _items.Clear();
        NotificarCambios();
    }

    private void NotificarCambios()
    {
        OnChange?.Invoke();
    }
}