using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace ExtensionesShop.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private const string USER_KEY = "currentUser";
    private const string TOKEN_KEY = "authToken";

    // Referencias a servicios que necesitan sincronizar
    private CartStateService? _cartService;
    private FavoritosService? _favoritosService;

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    /// <summary>
    /// Permite inyectar servicios después de la inicialización (para evitar dependencias circulares)
    /// </summary>
    public void SetDependencies(CartStateService cartService, FavoritosService favoritosService)
    {
        _cartService = cartService;
        _favoritosService = favoritosService;
    }

    public UserData? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.Role == "Admin";

    // Inicializar el servicio (llamar desde OnInitializedAsync en App o MainLayout)
    public async Task InitializeAsync()
    {
        try
        {
            var userJson = await _js.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
            if (!string.IsNullOrEmpty(userJson))
            {
                CurrentUser = JsonSerializer.Deserialize<UserData>(userJson);
                NotifyAuthStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing auth: {ex.Message}");
        }
    }

    // Login
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("api/users/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.User != null)
                {
                    CurrentUser = result.User;

                    // ✅ Guardar token JWT si viene en la respuesta
                    if (!string.IsNullOrEmpty(result.Token))
                    {
                        await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, result.Token);
                    }

                    await SaveUserToLocalStorage();

                    // 🔄 SINCRONIZAR CARRITO Y FAVORITOS
                    await SyncAfterLoginAsync();

                    NotifyAuthStateChanged();
                    return new AuthResult { Success = true };
                }
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return new AuthResult { Success = false, ErrorMessage = error?.Message ?? "Error al iniciar sesión" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    // Register
    public async Task<AuthResult> RegisterAsync(RegisterData data)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/users/register", data);

            if (response.IsSuccessStatusCode)
            {
                return new AuthResult { Success = true };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return new AuthResult { Success = false, ErrorMessage = error?.Message ?? "Error al registrar" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    // Logout
    public async Task LogoutAsync()
    {
        CurrentUser = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", USER_KEY);
        await _js.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY); // ✅ Limpiar token

        // 🔄 LIMPIAR CARRITO AL LOGOUT (volver a modo guest)
        if (_cartService != null)
        {
            await _cartService.OnLogoutAsync();
        }

        NotifyAuthStateChanged();
    }

    // Forgot Password
    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new { Email = email };
            var response = await _http.PostAsJsonAsync("api/users/forgot-password", request);

            if (response.IsSuccessStatusCode)
            {
                return new AuthResult { Success = true };
            }

            return new AuthResult { Success = false, ErrorMessage = "Error al procesar la solicitud" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    // Reset Password
    public async Task<AuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var request = new { Token = token, NewPassword = newPassword };
            var response = await _http.PostAsJsonAsync("api/users/reset-password", request);

            if (response.IsSuccessStatusCode)
            {
                return new AuthResult { Success = true };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return new AuthResult { Success = false, ErrorMessage = error?.Message ?? "Token inválido o expirado" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    // Update Profile
    public async Task<AuthResult> UpdateProfileAsync(int userId, UpdateProfileData data)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/users/profile/{userId}", data);

            if (response.IsSuccessStatusCode)
            {
                // Actualizar datos locales
                if (CurrentUser != null)
                {
                    CurrentUser.FirstName = data.FirstName;
                    CurrentUser.LastName = data.LastName;
                    CurrentUser.Phone = data.Phone;
                    CurrentUser.Address = data.Address;
                    CurrentUser.City = data.City;
                    CurrentUser.PostalCode = data.PostalCode;
                    await SaveUserToLocalStorage();
                    NotifyAuthStateChanged();
                }
                return new AuthResult { Success = true };
            }

            return new AuthResult { Success = false, ErrorMessage = "Error al actualizar perfil" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    private async Task SaveUserToLocalStorage()
    {
        if (CurrentUser != null)
        {
            var userJson = JsonSerializer.Serialize(CurrentUser);
            await _js.InvokeVoidAsync("localStorage.setItem", USER_KEY, userJson);
        }
    }

    /// <summary>
    /// Sincroniza carrito y favoritos después de hacer login
    /// </summary>
    private async Task SyncAfterLoginAsync()
    {
        try
        {
            // Sincronizar carrito local con backend
            if (_cartService != null)
            {
                await _cartService.SyncWithBackendAsync();
            }

            // Recargar favoritos desde backend
            if (_favoritosService != null)
            {
                await _favoritosService.ReloadFavoritesAsync();
            }

            Console.WriteLine("✅ Datos sincronizados correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sincronizando datos: {ex.Message}");
        }
    }

    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }
}

// Modelos
public class UserData
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string Role { get; set; } = "User";
}

public class RegisterData
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string RecaptchaToken { get; set; } = string.Empty;
}

public class UpdateProfileData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; } // ✅ JWT Token
    public UserData? User { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
