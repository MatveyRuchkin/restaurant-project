using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Services;

public class BusinessValidationService : IBusinessValidationService
{
    private readonly RestaurantDbContext _context;
    private readonly ILogger<BusinessValidationService> _logger;

    public BusinessValidationService(
        RestaurantDbContext context,
        ILogger<BusinessValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ValidateCategoryDeletionAsync(Guid categoryId)
    {
        var category = await _context.Categories
            .Include(c => c.Dishes)
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

        if (category == null)
        {
            throw new NotFoundException("Категория не найдена");
        }

        // Бизнес-правило: категорию нельзя удалить, если в ней есть активные блюда
        var activeDishes = category.Dishes.Where(d => !d.IsDeleted).ToList();
        if (activeDishes.Any())
        {
            _logger.LogWarning(
                "Попытка удаления категории {CategoryId} с активными блюдами. Количество блюд: {DishCount}",
                categoryId, activeDishes.Count);
            throw new BadRequestException(
                $"Невозможно удалить категорию '{category.Name}'. В ней содержится {activeDishes.Count} активных блюд. " +
                "Сначала удалите или переместите все блюда из этой категории.");
        }
    }

    public async Task ValidateDishDeletionAsync(Guid dishId)
    {
        var dish = await _context.Dishes
            .Include(d => d.OrderItems)
            .FirstOrDefaultAsync(d => d.Id == dishId && !d.IsDeleted);

        if (dish == null)
        {
            throw new NotFoundException("Блюдо не найдено");
        }

        // Бизнес-правило: блюдо нельзя удалить, если оно присутствует в активных заказах (Pending или Processing)
        var activeOrderItems = dish.OrderItems
            .Where(oi => !oi.IsDeleted)
            .Select(oi => oi.Order)
            .Where(o => !o.IsDeleted && 
                       (o.Status == OrderStatuses.Pending || o.Status == OrderStatuses.Processing))
            .ToList();

        if (activeOrderItems.Any())
        {
            _logger.LogWarning(
                "Попытка удаления блюда {DishId} из активных заказов. Количество активных заказов: {OrderCount}",
                dishId, activeOrderItems.Count);
            throw new BadRequestException(
                $"Невозможно удалить блюдо '{dish.Name}'. Оно присутствует в {activeOrderItems.Count} активных заказах. " +
                "Дождитесь завершения всех заказов с этим блюдом.");
        }
    }

    public async Task ValidateIngredientDeletionAsync(Guid ingredientId)
    {
        var ingredient = await _context.Ingredients
            .Include(i => i.DishIngredients)
                .ThenInclude(di => di.Dish)
            .FirstOrDefaultAsync(i => i.Id == ingredientId && !i.IsDeleted);

        if (ingredient == null)
        {
            throw new NotFoundException("Ингредиент не найден");
        }

        // Бизнес-правило: ингредиент нельзя удалить, если он используется в активных блюдах
        var activeDishes = ingredient.DishIngredients
            .Where(di => !di.IsDeleted && !di.Dish.IsDeleted)
            .Select(di => di.Dish)
            .Distinct()
            .ToList();

        if (activeDishes.Any())
        {
            _logger.LogWarning(
                "Попытка удаления ингредиента {IngredientId}, используемого в активных блюдах. Количество блюд: {DishCount}",
                ingredientId, activeDishes.Count);
            throw new BadRequestException(
                $"Невозможно удалить ингредиент '{ingredient.Name}'. Он используется в {activeDishes.Count} активных блюдах. " +
                "Сначала удалите ингредиент из всех блюд.");
        }
    }

    public async Task ValidateMenuDeletionAsync(Guid menuId)
    {
        var menu = await _context.Menus
            .Include(m => m.MenuDishes)
            .FirstOrDefaultAsync(m => m.Id == menuId && !m.IsDeleted);

        if (menu == null)
        {
            throw new NotFoundException("Меню не найдено");
        }

        // Меню можно удалить, блюда останутся в системе
        var activeMenuDishes = menu.MenuDishes.Where(md => !md.IsDeleted).ToList();
        if (activeMenuDishes.Any())
        {
            _logger.LogInformation(
                "Удаление меню {MenuId} с {Count} блюдами. Блюда останутся в системе",
                menuId, activeMenuDishes.Count);
        }
    }

    public async Task ValidateOrderStatusChangeAsync(Order order, string newStatus)
    {
        if (order == null)
        {
            throw new NotFoundException("Заказ не найден");
        }

        // Бизнес-правило: завершенный заказ нельзя вернуть в статус Pending или Processing
        if (order.Status == OrderStatuses.Completed && 
            (newStatus == OrderStatuses.Pending || newStatus == OrderStatuses.Processing))
        {
            _logger.LogWarning(
                "Попытка изменения статуса заказа {OrderId} с {OldStatus} на {NewStatus}",
                order.Id, order.Status, newStatus);
            throw new BadRequestException(
                "Невозможно изменить статус завершенного заказа обратно на 'Pending' или 'Processing'.");
        }

        // Бизнес-правило: завершенный заказ нельзя отменить
        if (order.Status == OrderStatuses.Completed && newStatus == OrderStatuses.Cancelled)
        {
            _logger.LogWarning(
                "Попытка отмены завершенного заказа {OrderId}",
                order.Id);
            throw new BadRequestException("Невозможно отменить завершенный заказ.");
        }

        if (!OrderStatuses.IsValid(newStatus))
        {
            throw new BadRequestException($"Недопустимый статус заказа: {newStatus}");
        }
    }

    public async Task ValidateOrderCreationAsync(OrderCreateDto orderDto)
    {
        if (orderDto.Items == null || !orderDto.Items.Any())
        {
            throw new BadRequestException("Заказ должен содержать хотя бы одно блюдо");
        }

        // Бизнес-правило: минимальная сумма заказа составляет 100 рублей
        const decimal minOrderTotal = 100m;
        decimal total = 0;

        foreach (var item in orderDto.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new BadRequestException($"Количество блюда должно быть больше 0");
            }

            if (item.Quantity > 100)
            {
                throw new BadRequestException($"Количество блюда не может превышать 100");
            }

            var dish = await _context.Dishes
                .FirstOrDefaultAsync(d => d.Id == item.DishId && !d.IsDeleted);

            if (dish == null)
            {
                throw new NotFoundException($"Блюдо с ID {item.DishId} не найдено или удалено");
            }

            total += dish.Price * item.Quantity;
        }

        if (total < minOrderTotal)
        {
            _logger.LogWarning(
                "Попытка создания заказа с суммой {Total}, минимальная сумма: {MinTotal}",
                total, minOrderTotal);
            throw new BadRequestException(
                $"Минимальная сумма заказа составляет {minOrderTotal} рублей. Текущая сумма: {total:F2} рублей.");
        }

        if (total < 0.01m)
        {
            throw new BadRequestException("Сумма заказа должна быть больше 0");
        }
    }
}

