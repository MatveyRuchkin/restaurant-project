using RestaurantAPI.Exceptions;

namespace RestaurantAPI.Services;

public interface IDishService
{
    Task ValidateDishDeletionAsync(Guid dishId);
    Task ValidateDishPriceChangeAsync(Guid dishId, decimal newPrice);
}

