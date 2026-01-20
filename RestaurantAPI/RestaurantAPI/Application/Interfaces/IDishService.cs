using RestaurantAPI.Exceptions;

namespace RestaurantAPI.Application.Interfaces;

public interface IDishService
{
    Task ValidateDishDeletionAsync(Guid dishId);
    Task ValidateDishPriceChangeAsync(Guid dishId, decimal newPrice);
}

