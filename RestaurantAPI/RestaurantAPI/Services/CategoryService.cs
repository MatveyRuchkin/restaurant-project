using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;

namespace RestaurantAPI.Services;

public class CategoryService : ICategoryService
{
    private readonly RestaurantDbContext _context;

    public CategoryService(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task ValidateCategoryDeletionAsync(Guid categoryId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);
        if (category == null)
        {
            throw new NotFoundException("Категория не найдена");
        }

        // Проверка наличия блюд в категории
        var dishes = await _context.Dishes
            .Where(d => d.CategoryId == categoryId && !d.IsDeleted)
            .ToListAsync();
        if (dishes.Any())
        {
            var dishesList = dishes.Select(d => d.Name).Take(5);
            var message = $"Нельзя удалить категорию '{category.Name}', так как в ней есть блюда: {string.Join(", ", dishesList)}";
            if (dishes.Count > 5)
            {
                message += $" и еще {dishes.Count - 5} блюд";
            }
            throw new BadRequestException(message);
        }
    }
}

