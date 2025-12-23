using RestaurantAPI.Exceptions;

namespace RestaurantAPI.Services;

public interface ICategoryService
{
    Task ValidateCategoryDeletionAsync(Guid categoryId);
}

