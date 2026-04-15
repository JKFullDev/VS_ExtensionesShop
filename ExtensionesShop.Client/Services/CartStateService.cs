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
    private readonly List<CarritoItemView> _items = new();
    private const string CART_KEY = "carrito_guest";

    // Evento para notificar cambios
    public event Action? OnChange;

    // Propiedades públicas
    public IReadOnlyList<CarritoItemView> Items => _items.AsReadOnly();
    public int CantidadTotal => _items.Sum(i => i.Cantidad);
    public decimal Total => _items.Sum(i => i.Producto.Price * i.Cantidad);
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
                    ProductVariantId = i.VariantId,  // ✅ NUEVO: Incluir ID de variante
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
    public async Task AgregarProducto(Product producto, int cantidad = 1, int? variantId = null)
    {
        if (producto == null || cantidad <= 0) return;

        if (_authService.IsAuthenticated)
        {
            // Usuario logueado - usar backend
            await AgregarProductoBackendAsync(producto, cantidad, variantId);
        }
        else
        {
            // Usuario guest - usar localStorage
            await AgregarProductoLocalAsync(producto, cantidad, variantId);  // ✅ PASAR variantId
        }
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito
    /// </summary>
    public async Task ActualizarCantidad(int productoId, int nuevaCantidad, int? variantId = null)
    {
        if (_authService.IsAuthenticated)
        {
            await ActualizarCantidadBackendAsync(productoId, nuevaCantidad, variantId);
        }
        else
        {
            await ActualizarCantidadLocalAsync(productoId, nuevaCantidad, variantId);
        }
    }

    /// <summary>
    /// Incrementa la cantidad de un producto
    /// </summary>
    public async Task IncrementarCantidad(int productoId, int? variantId = null)
    {
        // ✅ CRÍTICO: Buscar por ProductId Y VariantId
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId && i.VariantId == variantId);
        if (item != null)
        {
            await ActualizarCantidad(productoId, item.Cantidad + 1, variantId);
        }
    }

    /// <summary>
    /// Decrementa la cantidad de un producto
    /// </summary>
    public async Task DecrementarCantidad(int productoId, int? variantId = null)
    {
        // ✅ CRÍTICO: Buscar por ProductId Y VariantId
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId && i.VariantId == variantId);
        if (item != null && item.Cantidad > 1)
        {
            await ActualizarCantidad(productoId, item.Cantidad - 1, variantId);
        }
        else if (item != null && item.Cantidad == 1)
        {
            await EliminarProducto(productoId, variantId);
        }
    }

    /// <summary>
    /// Elimina un producto del carrito
    /// </summary>
    public async Task EliminarProducto(int productoId, int? variantId = null)
    {
        if (_authService.IsAuthenticated)
        {
            await EliminarProductoBackendAsync(productoId, variantId);  // ✅ PASAR variantId
        }
        else
        {
            await EliminarProductoLocalAsync(productoId, variantId);  // ✅ PASAR variantId
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

            var cartItems = await _http.GetFromJsonAsync<List<CartItemResponse>>("api/cart");

            if (cartItems != null)
            {
                Console.WriteLine($"✅ Carrito cargado: {cartItems.Count} items");
                _items.Clear();

                // Convertir CartItemResponse a CarritoItemView (estructura que el cliente espera)
                foreach (var cartItem in cartItems)
                {
                    var item = new CarritoItemView
                    {
                        Producto = new Product
                        {
                            Id = cartItem.ProductId,
                            Name = cartItem.ProductName,
                            ImageUrl = cartItem.ImageUrl,
                            Price = cartItem.UnitPrice,  // Usar el precio guardado
                            Color = cartItem.VariantColor ?? string.Empty,
                            Centimeters = cartItem.VariantCentimeters
                        },
                        Cantidad = cartItem.Quantity,
                        PrecioUnitario = cartItem.UnitPrice,  // ✅ GUARDAR PRECIO EN VISTA
                        VariantId = cartItem.ProductVariantId,  // ✅ GUARDAR ID VARIANTE
                        VariantColor = cartItem.VariantColor,  // ✅ GUARDAR COLOR
                        VariantCentimeters = cartItem.VariantCentimeters  // ✅ GUARDAR MEDIDA
                    };
                    _items.Add(item);

                    Console.WriteLine($"  📦 {cartItem.ProductName} x{cartItem.Quantity} (Variante: {cartItem.ProductVariantId}, Precio: ${cartItem.UnitPrice})");
                }

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

    private async Task AgregarProductoBackendAsync(Product producto, int cantidad, int? variantId = null)
    {
        try
        {
            var request = new AddToCartRequest
            {
                ProductId = producto.Id,
                ProductVariantId = variantId,
                Quantity = cantidad
            };

            Console.WriteLine($"🔵 Intentando añadir al carrito (backend): Producto {producto.Id}, Variante {variantId}, Cantidad {cantidad}");

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

    private async Task ActualizarCantidadBackendAsync(int productoId, int nuevaCantidad, int? variantId = null)
    {
        try
        {
            var request = new AddToCartRequest
            {
                ProductId = productoId,
                ProductVariantId = variantId,  // ✅ PASAR variantId
                Quantity = nuevaCantidad
            };

            Console.WriteLine($"🔵 Actualizando cantidad en backend: Producto {productoId}, Variante {variantId}, Cantidad {nuevaCantidad}");

            var response = await _http.PutAsJsonAsync($"api/cart/{productoId}", request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Cantidad actualizada exitosamente en backend");
                await LoadCartFromBackendAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al actualizar cantidad: {response.StatusCode} - {errorContent}");

                // Parsear y mostrar el mensaje de error del servidor
                try
                {
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<OperationResult>(errorContent);
                    if (errorResponse?.Message != null)
                    {
                        Console.WriteLine($"   Mensaje: {errorResponse.Message}");
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción actualizando cantidad (backend): {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }

    private async Task EliminarProductoBackendAsync(int productoId, int? variantId = null)
    {
        try
        {
            // ✅ PASAR variantId como query parameter
            var url = variantId.HasValue 
                ? $"api/cart/{productoId}?variantId={variantId}" 
                : $"api/cart/{productoId}";

            var response = await _http.DeleteAsync(url);
            
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
                var items = JsonSerializer.Deserialize<List<CarritoItemView>>(json);
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

    private async Task AgregarProductoLocalAsync(Product producto, int cantidad, int? variantId = null)
    {
        // ✅ CRÍTICO: Buscar por ProductId Y VariantId para no colapsar variantes
        var itemExistente = _items.FirstOrDefault(i => i.Producto.Id == producto.Id && i.VariantId == variantId);

        if (itemExistente != null)
        {
            itemExistente.Cantidad += cantidad;
        }
        else
        {
            _items.Add(new CarritoItemView
            {
                Producto = producto,
                Cantidad = cantidad,
                PrecioUnitario = producto.Price,
                VariantId = variantId  // ✅ NUEVO: Guardar ID de variante
            });
        }

        await SaveToLocalStorageAsync();
        NotificarCambios();
    }

    private async Task ActualizarCantidadLocalAsync(int productoId, int nuevaCantidad, int? variantId = null)
    {
        // ✅ CRÍTICO: Buscar por ProductId Y VariantId
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId && i.VariantId == variantId);
        if (item == null) return;

        if (nuevaCantidad <= 0)
        {
            await EliminarProductoLocalAsync(productoId, variantId);
        }
        else
        {
            item.Cantidad = nuevaCantidad;
            await SaveToLocalStorageAsync();
            NotificarCambios();
        }
    }

    private async Task EliminarProductoLocalAsync(int productoId, int? variantId = null)
    {
        // ✅ CRÍTICO: Buscar por ProductId Y VariantId
        var item = _items.FirstOrDefault(i => i.Producto.Id == productoId && i.VariantId == variantId);
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
