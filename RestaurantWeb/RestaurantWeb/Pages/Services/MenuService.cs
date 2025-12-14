using System.Net.Http.Json;
using RestaurantAPI.DTOs;

public class MenuService
{
    private readonly HttpClient _client;
    public MenuService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("RestaurantApi");
    }

    public async Task<List<MenuReadDto>> GetAllAsync()
    {
        return await _client.GetFromJsonAsync<List<MenuReadDto>>("Menus") ?? [];
    }
}
