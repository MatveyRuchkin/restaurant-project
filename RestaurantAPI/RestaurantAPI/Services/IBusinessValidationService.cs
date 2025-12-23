using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Services;

public interface IBusinessValidationService
{
    Task ValidateCategoryDeletionAsync(Guid categoryId);
    Task ValidateDishDeletionAsync(Guid dishId);
    Task ValidateIngredientDeletionAsync(Guid ingredientId);
    Task ValidateMenuDeletionAsync(Guid menuId);
    Task ValidateOrderStatusChangeAsync(Order order, string newStatus);
    Task ValidateOrderCreationAsync(OrderCreateDto orderDto);
}

