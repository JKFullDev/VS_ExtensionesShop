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
        int? categoryId = null,
        int? subcategoryId = null,
        string? search = null,
        int page = 1,
        int pageSize = 24)
    {
        var queryParams = new List<string>();

        if (categoryId.HasValue)
            queryParams.Add($"categoryId={categoryId.Value}");

        if (subcategoryId.HasValue)
            queryParams.Add($"subcategoryId={subcategoryId.Value}");

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");

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

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<Product>($"api/products/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar producto: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            var categories = await _http.GetFromJsonAsync<List<Category>>("api/categories");
            return categories ?? new List<Category>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar categorías: {ex.Message}");
            return new List<Category>();
        }
    }
}
