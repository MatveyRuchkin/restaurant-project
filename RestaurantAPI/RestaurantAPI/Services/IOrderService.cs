using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;

namespace RestaurantAPI.Services;

public interface IOrderService
{
    Task ValidateOrderCreationAsync(OrderCreateDto createDto);
    Task ValidateOrderStatusChangeAsync(Guid orderId, string newStatus, string currentStatus);
    Task ValidateOrderCancellationAsync(Guid orderId);
}

