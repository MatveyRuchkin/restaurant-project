using RestaurantAPI.DTOs;

namespace RestaurantAPI.Application.Interfaces;

public interface IOrderService
{
    Task ValidateOrderCreationAsync(OrderCreateDto createDto);
    Task ValidateOrderStatusChangeAsync(Guid orderId, string newStatus, string currentStatus);
    Task ValidateOrderCancellationAsync(Guid orderId);
}

