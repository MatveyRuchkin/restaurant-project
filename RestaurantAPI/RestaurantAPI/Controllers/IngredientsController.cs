using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;
using RestaurantAPI.DTOs;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(RestaurantDbContext context, ILogger<IngredientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Ingredients - доступно всем авторизованным
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetIngredients(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Ingredients
                    .Where(i => !i.IsDeleted);

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(i => i.Name.Contains(search));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(i => i.Name)
                        : query.OrderBy(i => i.Name),
                    _ => query.OrderBy(i => i.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var ingredients = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new IngredientReadDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список ингредиентов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    ingredients.Count, totalCount, page);

                return Ok(new
                {
                    data = ingredients,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ингредиентов");
                return StatusCode(500, "Произошла ошибка при получении списка ингредиентов.");
            }
        }

        // GET: api/Ingredients/5 - доступно всем авторизованным
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<IngredientReadDto>> GetIngredient(Guid id)
        {
            try
            {
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (ingredient == null)
                {
                    _logger.LogWarning("Ингредиент с Id {IngredientId} не найден", id);
                    return NotFound();
                }

                var dto = new IngredientReadDto
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении ингредиента {IngredientId}", id);
                return StatusCode(500, "Произошла ошибка при получении ингредиента.");
            }
        }

        // POST: api/Ingredients - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<IngredientReadDto>> CreateIngredient(IngredientCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Ingredients.AnyAsync(i => i.Name == createDto.Name && !i.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания ингредиента с существующим названием: {Name}", createDto.Name);
                    return BadRequest("Ингредиент с таким названием уже существует.");
                }

                var ingredient = new Ingredient
                {
                    Name = createDto.Name,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {Name} создан администратором {Username}",
                    ingredient.Name, username);

                var readDto = new IngredientReadDto
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name
                };

                return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании ингредиента {Name}", createDto.Name);
                return StatusCode(500, "Произошла ошибка при создании ингредиента.");
            }
        }

        // PUT: api/Ingredients/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateIngredient(Guid id, IngredientUpdateDto updateDto)
        {
            try
            {
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (ingredient == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего ингредиента: {IngredientId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Ingredients.AnyAsync(i =>
                    i.Name == updateDto.Name && i.Id != id && !i.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления ингредиента {IngredientId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    return BadRequest("Ингредиент с таким названием уже существует.");
                }

                ingredient.Name = updateDto.Name;
                ingredient.UpdatedAt = DateTime.UtcNow;
                ingredient.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientId} обновлен администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении ингредиента {IngredientId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении ингредиента.");
            }
        }

        // DELETE: api/Ingredients/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteIngredient(Guid id)
        {
            try
            {
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (ingredient == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего ингредиента: {IngredientId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                ingredient.IsDeleted = true;
                ingredient.DeletedAt = DateTime.UtcNow;
                ingredient.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientId} ({Name}) удален администратором {Username}",
                    id, ingredient.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении ингредиента {IngredientId}", id);
                return StatusCode(500, "Произошла ошибка при удалении ингредиента.");
            }
        }
    }
}
