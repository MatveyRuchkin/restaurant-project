using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Services;

public class OrderService : IOrderService
{
    private readonly RestaurantDbContext _context;
    private const decimal MIN_ORDER_TOTAL = 0.01m;
    private const int MAX_ITEM_QUANTITY = 100;

    public OrderService(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task ValidateOrderCreationAsync(OrderCreateDto createDto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == createDto.UserId && !u.IsDeleted);
        if (user == null)
        {
            throw new BadRequestException("Пользователь не найден");
        }

        if (user.IsDeleted)
        {
            throw new BadRequestException("Пользователь заблокирован");
        }

        if (createDto.Items == null || createDto.Items.Count == 0)
        {
            throw new BadRequestException("Заказ должен содержать хотя бы одно блюдо");
        }

        decimal total = 0;

        foreach (var itemDto in createDto.Items)
        {
            var dish = await _context.Dishes
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == itemDto.DishId && !d.IsDeleted);
            
            if (dish == null)
            {
                throw new BadRequestException($"Блюдо с ID {itemDto.DishId} не найдено или удалено");
            }

            if (dish.IsDeleted)
            {
                throw new BadRequestException($"Блюдо '{dish.Name}' недоступно");
            }

            if (itemDto.Quantity <= 0)
            {
                throw new BadRequestException($"Количество блюда '{dish.Name}' должно быть больше 0");
            }

            if (itemDto.Quantity > MAX_ITEM_QUANTITY)
            {
                throw new BadRequestException($"Количество блюда '{dish.Name}' не может превышать {MAX_ITEM_QUANTITY}");
            }

            total += dish.Price * itemDto.Quantity;
        }

        if (total < MIN_ORDER_TOTAL)
        {
            throw new BadRequestException($"Сумма заказа должна быть не менее {MIN_ORDER_TOTAL} руб.");
        }
    }

    public async Task ValidateOrderStatusChangeAsync(Guid orderId, string newStatus, string currentStatus)
    {
        if (!OrderStatuses.IsValid(newStatus))
        {
            throw new BadRequestException($"Недопустимый статус: {newStatus}");
        }

        // Бизнес-правило: завершенный заказ нельзя изменить
        if (currentStatus == OrderStatuses.Completed && newStatus != OrderStatuses.Completed)
        {
            throw new BadRequestException("Нельзя изменить статус завершенного заказа");
        }

        // Бизнес-правило: отмененный заказ нельзя изменить
        if (currentStatus == OrderStatuses.Cancelled && newStatus != OrderStatuses.Cancelled)
        {
            throw new BadRequestException("Нельзя изменить статус отмененного заказа");
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        if (order == null)
        {
            throw new NotFoundException("Заказ не найден");
        }
    }

    public async Task ValidateOrderCancellationAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        if (order == null)
        {
            throw new NotFoundException("Заказ не найден");
        }

        if (order.Status == OrderStatuses.Completed)
        {
            throw new BadRequestException("Нельзя отменить завершенный заказ");
        }

        if (order.Status == OrderStatuses.Cancelled)
        {
            throw new BadRequestException("Заказ уже отменен");
        }
    }
}

