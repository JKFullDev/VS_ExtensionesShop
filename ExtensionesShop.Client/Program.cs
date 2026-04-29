using ExtensionesShop.Client;
using ExtensionesShop.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ Añadir soporte de autorización
builder.Services.AddAuthorizationCore();

// ✅ Registrar AuthorizationMessageHandler
builder.Services.AddScoped<AuthorizationMessageHandler>();

// HttpClient apuntando al Server con Authorization automático
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };

    return httpClient;
});

// Registrar ProductService
builder.Services.AddScoped<ProductService>();

// Registrar AuthService
builder.Services.AddScoped<AuthService>();

// Registrar FavoritosService
builder.Services.AddScoped<FavoritosService>();

// Registrar CartStateService (Scoped para compatibilidad con HttpClient)
// En Blazor WASM, Scoped actúa como Singleton (toda la app corre en el cliente)
builder.Services.AddScoped<CartStateService>();

var host = builder.Build();

// Inicializar AuthService para cargar la sesión guardada
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

// Obtener servicios de carrito y favoritos
var cartService = host.Services.GetRequiredService<CartStateService>();
var favoritosService = host.Services.GetRequiredService<FavoritosService>();

// Configurar dependencias en AuthService (para evitar dependencias circulares)
authService.SetDependencies(cartService, favoritosService);

// Inicializar FavoritosService para cargar favoritos guardados
await favoritosService.InitializeAsync();

// Inicializar CartStateService para cargar el carrito guardado
await cartService.InitializeAsync();

await host.RunAsync();