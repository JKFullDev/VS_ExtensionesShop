using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace ExtensionesShop.Client.Services;

/// <summary>
/// Handler que añade automáticamente el token JWT a todas las peticiones HTTP
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    private const string TOKEN_KEY = "authToken";

    public AuthorizationMessageHandler(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener token del localStorage
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);

            if (!string.IsNullOrEmpty(token))
            {
                // Añadir token a la cabecera Authorization
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error añadiendo token: {ex.Message}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
