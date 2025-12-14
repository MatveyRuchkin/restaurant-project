using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(RestaurantDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Categories - доступно всем авторизованным
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetCategories(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Categories
                    .Where(c => !c.IsDeleted);

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.Name.Contains(search) ||
                                           (c.Notes != null && c.Notes.Contains(search)));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.Name)
                        : query.OrderBy(c => c.Name),
                    _ => query.OrderBy(c => c.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var categories = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CategoryReadDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Notes = c.Notes
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список категорий. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    categories.Count, totalCount, page);

                return Ok(new
                {
                    data = categories,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка категорий");
                return StatusCode(500, "Произошла ошибка при получении списка категорий.");
            }
        }

        // GET: api/Categories/5 - доступно всем авторизованным
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CategoryReadDto>> GetCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Категория с Id {CategoryId} не найдена", id);
                    return NotFound();
                }

                var dto = new CategoryReadDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Notes = category.Notes
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении категории {CategoryId}", id);
                return StatusCode(500, "Произошла ошибка при получении категории.");
            }
        }

        // POST: api/Categories - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<CategoryReadDto>> CreateCategory(CategoryCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Categories.AnyAsync(c => c.Name == createDto.Name && !c.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания категории с существующим названием: {Name}", createDto.Name);
                    return BadRequest("Категория с таким названием уже существует.");
                }

                var category = new Category
                {
                    Name = createDto.Name,
                    Notes = createDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {Name} создана администратором {Username}", category.Name, username);

                var readDto = new CategoryReadDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Notes = category.Notes
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании категории {Name}", createDto.Name);
                return StatusCode(500, "Произошла ошибка при создании категории.");
            }
        }

        // PUT: api/Categories/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, CategoryUpdateDto updateDto)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующей категории: {CategoryId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Categories.AnyAsync(c =>
                    c.Name == updateDto.Name && c.Id != id && !c.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления категории {CategoryId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    return BadRequest("Категория с таким названием уже существует.");
                }

                category.Name = updateDto.Name;
                category.Notes = updateDto.Notes;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {CategoryId} обновлена администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении категории {CategoryId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении категории.");
            }
        }

        // DELETE: api/Categories/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующей категории: {CategoryId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                category.IsDeleted = true;
                category.DeletedAt = DateTime.UtcNow;
                category.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {CategoryId} ({Name}) удалена администратором {Username}",
                    id, category.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении категории {CategoryId}", id);
                return StatusCode(500, "Произошла ошибка при удалении категории.");
            }
        }
    }
}
