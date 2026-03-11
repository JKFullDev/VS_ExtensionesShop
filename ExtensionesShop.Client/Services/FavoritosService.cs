using System.Net.Http.Json;
using System.Text.Json;
using ExtensionesShop.Shared.Models;
using Microsoft.JSInterop;

namespace ExtensionesShop.Client.Services;

public class FavoritosService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly AuthService _authService;
    private const string FAVORITES_KEY = "favoritos_guest"; // Para usuarios no logueados (backup)
    private List<int> _favoritosIdsCache = new();
    private List<Product> _favoritosCache = new();

    public event Action? OnChange;

    public FavoritosService(HttpClient http, IJSRuntime js, AuthService authService)
    {
        _http = http;
        _js = js;
        _authService = authService;
    }

    /// <summary>
    /// Inicializa el servicio cargando favoritos
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            if (_authService.IsAuthenticated)
            {
                // Usuario logueado - cargar desde backend
                await LoadFavoritesFromBackendAsync();
            }
            else
            {
                // Usuario guest - cargar desde localStorage (opcional, para mantener favoritos temporales)
                await LoadFavoritesFromLocalStorageAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inicializando favoritos: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la lista de productos favoritos
    /// </summary>
    public IReadOnlyList<Product> Favoritos => _favoritosCache.AsReadOnly();

    /// <summary>
    /// Cantidad de favoritos
    /// </summary>
    public int Count => _favoritosCache.Count;

    /// <summary>
    /// Verifica si un producto está en favoritos
    /// </summary>
    public bool IsFavorito(int productId)
    {
        return _favoritosIdsCache.Contains(productId);
    }

    /// <summary>
    /// Requiere autenticación para favoritos
    /// </summary>
    public bool RequiresAuth => !_authService.IsAuthenticated;

    /// <summary>
    /// Añade un producto a favoritos (requiere login)
    /// </summary>
    public async Task<bool> AddFavoritoAsync(int productId)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("⚠️ Se requiere iniciar sesión para añadir favoritos");
            return false;
        }

        try
        {
            var response = await _http.PostAsync($"api/favorites/{productId}", null);

            if (response.IsSuccessStatusCode)
            {
                if (!_favoritosIdsCache.Contains(productId))
                {
                    _favoritosIdsCache.Add(productId);
                    await LoadFavoritesFromBackendAsync(); // Recargar lista completa
                }

                NotifyStateChanged();
                return true;
            }

            var error = await response.Content.ReadFromJsonAsync<OperationResult>();
            Console.WriteLine($"Error añadiendo favorito: {error?.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error añadiendo favorito: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Añade un producto completo a favoritos (requiere login)
    /// </summary>
    public async Task<bool> AddFavoritoAsync(Product producto)
    {
        if (producto == null)
            return false;

        var added = await AddFavoritoAsync(producto.Id);

        if (added && !_favoritosCache.Any(p => p.Id == producto.Id))
        {
            _favoritosCache.Add(producto);
            NotifyStateChanged();
        }

        return added;
    }

    /// <summary>
    /// Quita un producto de favoritos (requiere login)
    /// </summary>
    public async Task<bool> RemoveFavoritoAsync(int productId)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("⚠️ Se requiere iniciar sesión");
            return false;
        }

        try
        {
            var response = await _http.DeleteAsync($"api/favorites/{productId}");

            if (response.IsSuccessStatusCode)
            {
                _favoritosIdsCache.Remove(productId);
                var producto = _favoritosCache.FirstOrDefault(p => p.Id == productId);
                if (producto != null)
                {
                    _favoritosCache.Remove(producto);
                }

                NotifyStateChanged();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error quitando favorito: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Alterna el estado de favorito de un producto (requiere login)
    /// </summary>
    public async Task<bool> ToggleFavoritoAsync(int productId)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("⚠️ Se requiere iniciar sesión para usar favoritos");
            return false;
        }

        if (IsFavorito(productId))
        {
            await RemoveFavoritoAsync(productId);
            return false;
        }
        else
        {
            await AddFavoritoAsync(productId);
            return true;
        }
    }

    /// <summary>
    /// Alterna el estado de favorito de un producto completo (requiere login)
    /// </summary>
    public async Task<bool> ToggleFavoritoAsync(Product producto)
    {
        if (producto == null)
            return false;

        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("⚠️ Se requiere iniciar sesión para usar favoritos");
            return false;
        }

        if (IsFavorito(producto.Id))
        {
            await RemoveFavoritoAsync(producto.Id);
            return false;
        }
        else
        {
            await AddFavoritoAsync(producto);
            return true;
        }
    }

    /// <summary>
    /// Limpia todos los favoritos (requiere login)
    /// </summary>
    public async Task ClearFavoritosAsync()
    {
        if (!_authService.IsAuthenticated)
            return;

        try
        {
            var response = await _http.DeleteAsync("api/favorites");

            if (response.IsSuccessStatusCode)
            {
                _favoritosIdsCache.Clear();
                _favoritosCache.Clear();
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error limpiando favoritos: {ex.Message}");
        }
    }

    /// <summary>
    /// Recarga los favoritos desde el backend (solo si está logueado)
    /// </summary>
    public async Task ReloadFavoritesAsync()
    {
        if (_authService.IsAuthenticated)
        {
            await LoadFavoritesFromBackendAsync();
        }
    }

    // ===== Métodos Privados =====

    private async Task LoadFavoritesFromBackendAsync()
    {
        try
        {
            // Cargar productos favoritos completos
            var favorites = await _http.GetFromJsonAsync<List<Product>>("api/favorites");

            if (favorites != null)
            {
                _favoritosCache = favorites;
                _favoritosIdsCache = favorites.Select(p => p.Id).ToList();
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando favoritos desde backend: {ex.Message}");
            _favoritosCache.Clear();
            _favoritosIdsCache.Clear();
        }
    }

    private async Task LoadFavoritesFromLocalStorageAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", FAVORITES_KEY);

            if (!string.IsNullOrEmpty(json))
            {
                var ids = JsonSerializer.Deserialize<List<int>>(json);
                if (ids != null)
                {
                    _favoritosIdsCache = ids;
                    // No cargamos productos completos para guest users
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando favoritos de localStorage: {ex.Message}");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
