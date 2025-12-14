using System.Net.Http.Json;
using RestaurantAPI.DTOs;

public class DishService
{
    private readonly HttpClient _client;
    public DishService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("RestaurantApi");
    }

    public async Task<List<DishReadDto>> GetDishesForMenuAsync(Guid menuId)
    {
        var menuDishes = await _client.GetFromJsonAsync<List<MenuDishReadDto>>("MenuDishes") ?? [];
        var dishIds = menuDishes.Where(md => md.MenuId == menuId).Select(md => md.DishId).ToList();

        var allDishes = await _client.GetFromJsonAsync<List<DishReadDto>>("Dishes") ?? [];
        return allDishes.Where(d => dishIds.Contains(d.Id)).ToList();
    }
}
