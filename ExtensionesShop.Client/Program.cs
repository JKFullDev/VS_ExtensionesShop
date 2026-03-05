using ExtensionesShop.Client;
using ExtensionesShop.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient apuntando al Server
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Registrar ProductService
builder.Services.AddScoped<ProductService>();

// Registrar CartStateService como Singleton para mantener el estado
builder.Services.AddSingleton<CartStateService>();

await builder.Build().RunAsync();