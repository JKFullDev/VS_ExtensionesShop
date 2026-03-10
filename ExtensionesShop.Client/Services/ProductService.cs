using ExtensionesShop.Shared.Models;
using System.Net.Http.Json;

namespace ExtensionesShop.Client.Services;

public class ProductService
{
    private readonly HttpClient _http;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Product>> GetProductsAsync(
        string? category = null,
        string? search = null,
        bool? featured = null,
        int page = 1,
        int pageSize = 24)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");

        if (featured.HasValue)
            queryParams.Add($"featured={featured.Value}");

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var query = string.Join("&", queryParams);
        var url = $"api/products?{query}";

        try
        {
            Console.WriteLine($"🌐 Llamando a: {url}");
            Console.WriteLine($"🌐 BaseAddress: {_http.BaseAddress}");

            var products = await _http.GetFromJsonAsync<List<Product>>(url);

            Console.WriteLine($"📦 Respuesta recibida. Productos: {products?.Count ?? 0}");

            return products ?? new List<Product>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cargar productos: {ex.Message}");
            Console.WriteLine($"❌ Tipo: {ex.GetType().Name}");
            Console.WriteLine($"❌ Stack: {ex.StackTrace}");
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductBySlugAsync(string slug)
    {
        try
        {
            return await _http.GetFromJsonAsync<Product>($"api/products/{slug}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar producto: {ex.Message}");
            return null;
        }
    }
}