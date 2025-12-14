using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<MenusController> _logger;

        public MenusController(RestaurantDbContext context, ILogger<MenusController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Menus - доступно всем авторизованным
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetMenus(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Menus
                    .Where(m => !m.IsDeleted);

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m => m.Name.Contains(search));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(m => m.Name)
                        : query.OrderBy(m => m.Name),
                    _ => query.OrderBy(m => m.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var menus = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new MenuReadDto
                    {
                        Id = m.Id,
                        Name = m.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список меню. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    menus.Count, totalCount, page);

                return Ok(new
                {
                    data = menus,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка меню");
                return StatusCode(500, "Произошла ошибка при получении списка меню.");
            }
        }

        // GET: api/Menus/5 - доступно всем авторизованным
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<MenuReadDto>> GetMenu(Guid id)
        {
            try
            {
                var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (menu == null)
                {
                    _logger.LogWarning("Меню с Id {MenuId} не найдено", id);
                    return NotFound();
                }

                var dto = new MenuReadDto
                {
                    Id = menu.Id,
                    Name = menu.Name
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении меню {MenuId}", id);
                return StatusCode(500, "Произошла ошибка при получении меню.");
            }
        }

        // POST: api/Menus - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<MenuReadDto>> CreateMenu(MenuCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Menus.AnyAsync(m => m.Name == createDto.Name && !m.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания меню с существующим названием: {Name}", createDto.Name);
                    return BadRequest("Меню с таким названием уже существует.");
                }

                var menu = new Menu
                {
                    Name = createDto.Name,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Menus.Add(menu);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {Name} создано администратором {Username}", menu.Name, username);

                var readDto = new MenuReadDto
                {
                    Id = menu.Id,
                    Name = menu.Name
                };

                return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании меню {Name}", createDto.Name);
                return StatusCode(500, "Произошла ошибка при создании меню.");
            }
        }

        // PUT: api/Menus/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateMenu(Guid id, MenuUpdateDto updateDto)
        {
            try
            {
                var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего меню: {MenuId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Menus.AnyAsync(m =>
                    m.Name == updateDto.Name && m.Id != id && !m.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления меню {MenuId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    return BadRequest("Меню с таким названием уже существует.");
                }

                menu.Name = updateDto.Name;
                menu.UpdatedAt = DateTime.UtcNow;
                menu.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {MenuId} обновлено администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении меню {MenuId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении меню.");
            }
        }

        // DELETE: api/Menus/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            try
            {
                var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего меню: {MenuId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                menu.IsDeleted = true;
                menu.DeletedAt = DateTime.UtcNow;
                menu.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {MenuId} ({Name}) удалено администратором {Username}",
                    id, menu.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении меню {MenuId}", id);
                return StatusCode(500, "Произошла ошибка при удалении меню.");
            }
        }
    }
}