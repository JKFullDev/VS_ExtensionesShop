using System.Net.Http.Json;
using ExtensionesShop.Shared.Models;

namespace ExtensionesShop.Client.Services;

public class OrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Order?> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/orders", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Order>();
        }
        return null;
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _http.GetFromJsonAsync<List<Order>>($"api/orders?userId={userId}") ?? new();
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _http.GetFromJsonAsync<Order>($"api/orders/{orderId}");
    }
}
