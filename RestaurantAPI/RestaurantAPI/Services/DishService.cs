using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;

namespace RestaurantAPI.Services;

public class DishService : IDishService
{
    private readonly RestaurantDbContext _context;

    public DishService(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task ValidateDishDeletionAsync(Guid dishId)
    {
        var dish = await _context.Dishes
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == dishId && !d.IsDeleted);
        if (dish == null)
        {
            throw new NotFoundException("Блюдо не найдено");
        }

        // Проверка наличия блюда в активных заказах (Pending или Processing)
        var activeOrdersCount = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.DishId == dishId && 
                        !oi.IsDeleted && 
                        !oi.Order.IsDeleted &&
                        (oi.Order.Status == "Pending" || oi.Order.Status == "Processing"))
            .CountAsync();

        if (activeOrdersCount > 0)
        {
            throw new BadRequestException($"Нельзя удалить блюдо '{dish.Name}', так как оно есть в {activeOrdersCount} активных заказах");
        }
    }

    public async Task ValidateDishPriceChangeAsync(Guid dishId, decimal newPrice)
    {
        var dish = await _context.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId && !d.IsDeleted);
        if (dish == null)
        {
            throw new NotFoundException("Блюдо не найдено");
        }

        // Предупреждение при изменении цены более чем на 50% (не блокирует операцию)
        if (dish.Price > 0)
        {
            var priceChangePercent = Math.Abs((newPrice - dish.Price) / dish.Price) * 100;
            if (priceChangePercent > 50)
            {
                // В продакшене здесь можно добавить требование подтверждения администратора
            }
        }

        if (newPrice <= 0)
        {
            throw new BadRequestException("Цена блюда должна быть больше 0");
        }
    }
}

