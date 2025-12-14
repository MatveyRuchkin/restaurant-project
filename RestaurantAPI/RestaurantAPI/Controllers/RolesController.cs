using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using System.Security.Claims;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")] // Весь контроллер только для админов
    public class RolesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(RestaurantDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Roles
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult> GetRoles(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Roles
                    .Where(r => !r.IsDeleted);

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(r => r.Name.Contains(search));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(r => r.Name)
                        : query.OrderBy(r => r.Name),
                    _ => query.OrderBy(r => r.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var roles = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RoleReadDto
                    {
                        Id = r.Id,
                        Name = r.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список ролей. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    roles.Count, totalCount, page);

                return Ok(new
                {
                    data = roles,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ролей");
                return StatusCode(500, "Произошла ошибка при получении списка ролей.");
            }
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(Guid id)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

                if (role == null)
                {
                    _logger.LogWarning("Роль с Id {RoleId} не найдена", id);
                    return NotFound();
                }

                var dto = new RoleReadDto
                {
                    Id = role.Id,
                    Name = role.Name
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении роли {RoleId}", id);
                return StatusCode(500, "Произошла ошибка при получении роли.");
            }
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole(RoleCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Roles.AnyAsync(r => r.Name == createDto.Name && !r.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания роли с существующим названием: {Name}", createDto.Name);
                    return BadRequest("Роль с таким названием уже существует.");
                }

                var role = new Role
                {
                    Name = createDto.Name,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {Name} создана администратором {Username}", role.Name, username);

                var readDto = new RoleReadDto
                {
                    Id = role.Id,
                    Name = role.Name
                };

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании роли {Name}", createDto.Name);
                return StatusCode(500, "Произошла ошибка при создании роли.");
            }
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, RoleUpdateDto updateDto)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующей роли: {RoleId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                // Проверка на дубликат
                if (await _context.Roles.AnyAsync(r =>
                    r.Name == updateDto.Name && r.Id != id && !r.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления роли {RoleId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    return BadRequest("Роль с таким названием уже существует.");
                }

                role.Name = updateDto.Name;
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {RoleId} обновлена администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении роли {RoleId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении роли.");
            }
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующей роли: {RoleId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                role.IsDeleted = true;
                role.DeletedAt = DateTime.UtcNow;
                role.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {RoleId} ({Name}) удалена администратором {Username}",
                    id, role.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении роли {RoleId}", id);
                return StatusCode(500, "Произошла ошибка при удалении роли.");
            }
        }
    }
}