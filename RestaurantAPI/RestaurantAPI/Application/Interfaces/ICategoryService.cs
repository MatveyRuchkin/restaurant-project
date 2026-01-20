namespace RestaurantAPI.Application.Interfaces;

public interface ICategoryService
{
    Task ValidateCategoryDeletionAsync(Guid categoryId);
}

