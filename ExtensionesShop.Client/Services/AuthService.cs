using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace ExtensionesShop.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private const string USER_KEY = "currentUser";

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public UserData? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

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
                    await SaveUserToLocalStorage();
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
    public string? PostalCode { get; set; }
}

public class RegisterData
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateProfileData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
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
    public UserData? User { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
