using System.Net.Http.Json;
using System.Text.Json;
using ExtensionesShop.Shared.Models;
using Microsoft.JSInterop;

namespace ExtensionesShop.Client.Services;

public class CartStateService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly AuthService _authService;
    private readonly List<CarritoItem> _items = new();
    private const string CART_KEY = "carrito_guest";

    // Evento para notificar cambios
    public event Action? OnChange;

    // Propiedades públicas
    public IReadOnlyList<CarritoItem> Items => _items.AsReadOnly();
    public int CantidadTotal => _items.Sum(i => i.Cantidad);
    public decimal Total => _items.Sum(i => i.Subtotal);
    public bool IsGuest => !_authService.IsAuthenticated;

    public CartStateService(HttpClient http, IJSRuntime js, AuthService authService)
    {
        _http = http;
        _js = js;
        _authService = authService;
    }

    /// <summary>
    /// Inicializa el carrito cargando desde localStorage o backend
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            if (_authService.IsAuthenticated)
            {
                // Usuario logueado - cargar desde backend
                await LoadCartFromBackendAsync();
            }
            else
            {
                // Usuario guest - cargar desde localStorage
                await LoadCartFromLocalStorageAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inicializando carrito: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincroniza el carrito local con el backend (al hacer login)
    /// </summary>
    public async Task SyncWithBackendAsync()
    {
        if (!_authService.IsAuthenticated || !_items.Any())
            return;

        try
        {
            var syncRequest = new SyncCartRequest
            {
                Items = _items.Select(i => new CartItemSync
                {
                    ProductId = i.Producto.Id,
                    Quantity = i.Cantidad
                }).ToList()
            };

            var response = await _http.PostAsJsonAsync("api/cart/sync", syncRequest);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Carrito sincronizado con backend");
                
                // Limpiar localStorage después de sincronizar
                await ClearLocalStorageAsync();
                
                // Recargar desde backend
                await LoadCartFromBackendAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sincronizando carrito: {ex.Message}");
        }
    }

    /// <summary>
    /// Añade un producto al carrito
    /// </summary>
    public async Task AgregarProducto(Product producto, int cantidad = 1)
    {
        if (producto == null || cantidad <= 0) return;

        if (_authService.IsAuthenticated)
        {
            // Usuario logueado - usar backend
            await AgregarProductoBackendAsync(producto, cantidad);
        }
        else
        {
            // Usuario guest - usar localStorage
            await AgregarProductoLocalAsync(producto, cantidad);
        }
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito
    /// </summary>
    public async Task ActualizarCantidad(int productoId, int nuevaCantidad)
    {
        if (_authService.IsAuthenticated)
        {
            await ActualizarCantidadBackendAsync(productoId, nuevaCantidad);
        }
        else
        {
            await ActualizarCantidadLocalAsync(productoId, nuevaCantidad);
        }
    }

    /// <summary>
    /// Incrementa la cantidad de un producto
    /// </summary>
    public async Task IncrementarCantidad(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null)
        {
            await ActualizarCantidad(productoId, item.Cantidad + 1);
        }
    }

    /// <summary>
    /// Decrementa la cantidad de un producto
    /// </summary>
    public async Task DecrementarCantidad(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null && item.Cantidad > 1)
        {
            await ActualizarCantidad(productoId, item.Cantidad - 1);
        }
        else if (item != null && item.Cantidad == 1)
        {
            await EliminarProducto(productoId);
        }
    }

    /// <summary>
    /// Elimina un producto del carrito
    /// </summary>
    public async Task EliminarProducto(int productoId)
    {
        if (_authService.IsAuthenticated)
        {
            await EliminarProductoBackendAsync(productoId);
        }
        else
        {
            await EliminarProductoLocalAsync(productoId);
        }
    }

    /// <summary>
    /// Vacía completamente el carrito
    /// </summary>
    public async Task VaciarCarrito()
    {
        if (_authService.IsAuthenticated)
        {
            await VaciarCarritoBackendAsync();
        }
        else
        {
            await VaciarCarritoLocalAsync();
        }
    }

    /// <summary>
    /// Limpia el carrito local al hacer logout
    /// </summary>
    public async Task OnLogoutAsync()
    {
        _items.Clear();
        await LoadCartFromLocalStorageAsync();
        NotificarCambios();
    }

    // ===== Métodos BACKEND =====

    private async Task LoadCartFromBackendAsync()
    {
        try
        {
            Console.WriteLine("🔵 Cargando carrito desde backend...");

            var cartItems = await _http.GetFromJsonAsync<List<CarritoItem>>("api/cart");

            if (cartItems != null)
            {
                Console.WriteLine($"✅ Carrito cargado: {cartItems.Count} items");
                _items.Clear();
                _items.AddRange(cartItems);
                NotificarCambios();
            }
            else
            {
                Console.WriteLine("⚠️ El backend devolvió null");
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"❌ Error HTTP cargando carrito desde backend: {httpEx.StatusCode} - {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando carrito desde backend: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task AgregarProductoBackendAsync(Product producto, int cantidad)
    {
        try
        {
            var request = new AddToCartRequest
            {
                ProductId = producto.Id,
                Quantity = cantidad
            };

            Console.WriteLine($"🔵 Intentando añadir al carrito (backend): Producto {producto.Id}, Cantidad {cantidad}");

            var response = await _http.PostAsJsonAsync("api/cart", request);

            Console.WriteLine($"📡 Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Producto añadido exitosamente al carrito (backend)");
                await LoadCartFromBackendAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al añadir al carrito: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción añadiendo producto al carrito (backend): {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task ActualizarCantidadBackendAsync(int productoId, int nuevaCantidad)
    {
        try
        {
            var request = new AddToCartRequest
            {
                ProductId = productoId,
                Quantity = nuevaCantidad
            };

            var response = await _http.PutAsJsonAsync($"api/cart/{productoId}", request);
            
            if (response.IsSuccessStatusCode)
            {
                await LoadCartFromBackendAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error actualizando cantidad (backend): {ex.Message}");
        }
    }

    private async Task EliminarProductoBackendAsync(int productoId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/cart/{productoId}");
            
            if (response.IsSuccessStatusCode)
            {
                await LoadCartFromBackendAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error eliminando producto (backend): {ex.Message}");
        }
    }

    private async Task VaciarCarritoBackendAsync()
    {
        try
        {
            var response = await _http.DeleteAsync("api/cart");
            
            if (response.IsSuccessStatusCode)
            {
                _items.Clear();
                NotificarCambios();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error vaciando carrito (backend): {ex.Message}");
        }
    }

    // ===== Métodos LOCALSTORAGE (GUEST) =====

    private async Task LoadCartFromLocalStorageAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", CART_KEY);
            
            if (!string.IsNullOrEmpty(json))
            {
                var items = JsonSerializer.Deserialize<List<CarritoItem>>(json);
                if (items != null)
                {
                    _items.Clear();
                    _items.AddRange(items);
                    NotificarCambios();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando carrito de localStorage: {ex.Message}");
        }
    }

    private async Task AgregarProductoLocalAsync(Product producto, int cantidad)
    {
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

        await SaveToLocalStorageAsync();
        NotificarCambios();
    }

    private async Task ActualizarCantidadLocalAsync(int productoId, int nuevaCantidad)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item == null) return;

        if (nuevaCantidad <= 0)
        {
            await EliminarProductoLocalAsync(productoId);
        }
        else
        {
            item.Cantidad = nuevaCantidad;
            await SaveToLocalStorageAsync();
            NotificarCambios();
        }
    }

    private async Task EliminarProductoLocalAsync(int productoId)
    {
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId);
        if (item != null)
        {
            _items.Remove(item);
            await SaveToLocalStorageAsync();
            NotificarCambios();
        }
    }

    private async Task VaciarCarritoLocalAsync()
    {
        _items.Clear();
        await SaveToLocalStorageAsync();
        NotificarCambios();
    }

    private async Task SaveToLocalStorageAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items);
            await _js.InvokeVoidAsync("localStorage.setItem", CART_KEY, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error guardando carrito: {ex.Message}");
        }
    }

    private async Task ClearLocalStorageAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", CART_KEY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error limpiando localStorage: {ex.Message}");
        }
    }

    private void NotificarCambios() => OnChange?.Invoke();
}
