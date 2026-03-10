using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace ExtensionesShop.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private const string USER_KEY = "currentUser";

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public event Action? OnAuthStateChanged;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (result?.Success == true && result.User != null)
        {
            await SetUserAsync(result.User);
            OnAuthStateChanged?.Invoke();
        }

        return result ?? new AuthResponse { Success = false, Message = "Error en el registro" };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (result?.Success == true && result.User != null)
        {
            await SetUserAsync(result.User);
            OnAuthStateChanged?.Invoke();
        }

        return result ?? new AuthResponse { Success = false, Message = "Error en el inicio de sesión" };
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", USER_KEY);
        OnAuthStateChanged?.Invoke();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
            if (string.IsNullOrEmpty(json))
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<UserDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/auth/user/{userId}", request);
        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user != null)
            {
                await SetUserAsync(user);
                OnAuthStateChanged?.Invoke();
            }
            return user;
        }
        return null;
    }

    private async Task SetUserAsync(UserDto user)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(user);
        await _js.InvokeVoidAsync("localStorage.setItem", USER_KEY, json);
    }
}

// DTOs (deben coincidir con el servidor)
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

public class UserDto
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

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto? User { get; set; }
}
