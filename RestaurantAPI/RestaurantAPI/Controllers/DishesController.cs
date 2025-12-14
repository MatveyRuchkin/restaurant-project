using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<DishesController> _logger;

        public DishesController(RestaurantDbContext context, ILogger<DishesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Dishes - доступно всем авторизованным
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetDishes(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Dishes
                    .Include(d => d.Category)
                    .Where(d => !d.IsDeleted);

                // Фильтрация по категории
                if (categoryId.HasValue)
                {
                    query = query.Where(d => d.CategoryId == categoryId.Value);
                }

                // Фильтрация по цене
                if (minPrice.HasValue)
                {
                    query = query.Where(d => d.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(d => d.Price <= maxPrice.Value);
                }

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || 
                                           (d.Description != null && d.Description.Contains(search)));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "price" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Price)
                        : query.OrderBy(d => d.Price),
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Name)
                        : query.OrderBy(d => d.Name),
                    "category" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Category.Name)
                        : query.OrderBy(d => d.Category.Name),
                    _ => query.OrderBy(d => d.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var dishes = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DishReadDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        Price = d.Price,
                        CategoryName = d.Category.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список блюд. Количество: {Count}, Всего: {Total}, Страница: {Page}, Размер страницы: {PageSize}",
                    dishes.Count, totalCount, page, pageSize);

                return Ok(new
                {
                    data = dishes,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка блюд");
                return StatusCode(500, "Произошла ошибка при получении списка блюд.");
            }
        }

        // GET: api/Dishes/5 - доступно всем авторизованным
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<DishReadDto>> GetDish(Guid id)
        {
            try
            {
                var dish = await _context.Dishes
                    .Include(d => d.Category)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dish == null)
                {
                    _logger.LogWarning("Блюдо с Id {DishId} не найдено", id);
                    return NotFound();
                }

                var dto = new DishReadDto
                {
                    Id = dish.Id,
                    Name = dish.Name,
                    Description = dish.Description,
                    Price = dish.Price,
                    CategoryName = dish.Category.Name
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении блюда {DishId}", id);
                return StatusCode(500, "Произошла ошибка при получении блюда.");
            }
        }

        // POST: api/Dishes - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<DishReadDto>> CreateDish(DishCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Dishes.AnyAsync(d => d.Name == createDto.Name && !d.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания блюда с существующим названием: {Name}", createDto.Name);
                    return BadRequest("Блюдо с таким названием уже существует.");
                }

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка создания блюда с несуществующей категорией: {CategoryId}", createDto.CategoryId);
                    return BadRequest("Категория не найдена.");
                }

                var dish = new Dish
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    Price = createDto.Price,
                    CategoryId = createDto.CategoryId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Dishes.Add(dish);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {Name} создано администратором {Username}", dish.Name, username);

                var readDto = new DishReadDto
                {
                    Id = dish.Id,
                    Name = dish.Name,
                    Description = dish.Description,
                    Price = dish.Price,
                    CategoryName = category.Name
                };

                return CreatedAtAction(nameof(GetDish), new { id = dish.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании блюда {Name}", createDto.Name);
                return StatusCode(500, "Произошла ошибка при создании блюда.");
            }
        }

        // PUT: api/Dishes/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateDish(Guid id, DishUpdateDto updateDto)
        {
            try
            {
                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего блюда: {DishId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Dishes.AnyAsync(d => 
                    d.Name == updateDto.Name && d.Id != id && !d.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления блюда {DishId} с существующим названием: {Name}", 
                        id, updateDto.Name);
                    return BadRequest("Блюдо с таким названием уже существует.");
                }

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка обновления блюда {DishId} с несуществующей категорией: {CategoryId}", 
                        id, updateDto.CategoryId);
                    return BadRequest("Категория не найдена.");
                }

                dish.Name = updateDto.Name;
                dish.Description = updateDto.Description;
                dish.Price = updateDto.Price;
                dish.CategoryId = updateDto.CategoryId;
                dish.UpdatedAt = DateTime.UtcNow;
                dish.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishId} обновлено администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении блюда {DishId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении блюда.");
            }
        }

        // DELETE: api/Dishes/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteDish(Guid id)
        {
            try
            {
                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего блюда: {DishId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                dish.IsDeleted = true;
                dish.DeletedAt = DateTime.UtcNow;
                dish.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishId} ({Name}) удалено администратором {Username}", 
                    id, dish.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении блюда {DishId}", id);
                return StatusCode(500, "Произошла ошибка при удалении блюда.");
            }
        }
    }
}
