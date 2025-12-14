using System.Net.Http.Json;
using RestaurantAPI.DTOs;

public class OrderService
{
    private readonly HttpClient _client;

    public OrderService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("RestaurantApi");
    }

    // ============================
    // Получить все заказы
    // ============================
    public async Task<List<OrderReadDto>> GetAllOrdersAsync()
    {
        return await _client.GetFromJsonAsync<List<OrderReadDto>>("Orders")
            ?? new List<OrderReadDto>();
    }

    // ============================
    // Получить заказ по Id
    // ============================
    public async Task<OrderReadDto?> GetOrderByIdAsync(Guid id)
    {
        return await _client.GetFromJsonAsync<OrderReadDto>($"Orders/{id}");
    }

    // ============================
    // Создать заказ
    // ============================
    public async Task<OrderReadDto?> CreateOrderAsync(OrderCreateDto dto)
    {
        var response = await _client.PostAsJsonAsync("Orders", dto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OrderReadDto>();
    }

    // ============================
    // Обновить статус заказа
    // ============================
    public async Task<bool> UpdateOrderStatusAsync(Guid id, OrderUpdateDto dto)
    {
        var response = await _client.PutAsJsonAsync($"Orders/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    // ============================
    // Удалить заказ
    // ============================
    public async Task<bool> DeleteOrderAsync(Guid id)
    {
        var response = await _client.DeleteAsync($"Orders/{id}");
        return response.IsSuccessStatusCode;
    }

    // ============================
    // Добавить позицию (item) в заказ
    // ============================
    public async Task<OrderItemReadDto?> AddItemToOrderAsync(Guid orderId, OrderItemCreateDto dto)
    {
        var response = await _client.PostAsJsonAsync($"Orders/{orderId}/Items", dto);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OrderItemReadDto>();
    }

    // ============================
    // Обновить количество позиции
    // ============================
    public async Task<bool> UpdateOrderItemAsync(Guid orderId, Guid itemId, OrderItemUpdateDto dto)
    {
        var response = await _client.PutAsJsonAsync(
            $"Orders/{orderId}/Items/{itemId}", dto);

        return response.IsSuccessStatusCode;
    }

    // ============================
    // Удалить позицию из заказа
    // ============================
    public async Task<bool> DeleteOrderItemAsync(Guid orderId, Guid itemId)
    {
        var response = await _client.DeleteAsync($"Orders/{orderId}/Items/{itemId}");
        return response.IsSuccessStatusCode;
    }
}
