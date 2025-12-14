using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using System.Security.Claims;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            RestaurantDbContext context,
            IPasswordService passwordService,
            ILogger<UsersController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        // GET: api/Users
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult> GetUsers(
            Guid? roleId = null,
            string? search = null,
            string sortBy = "username",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Role)
                    .Where(u => !u.IsDeleted);

                // Фильтрация по роли
                if (roleId.HasValue)
                {
                    query = query.Where(u => u.RoleId == roleId.Value);
                }

                // Поиск по имени пользователя
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u => u.Username.Contains(search));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "username" => order.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.Username)
                        : query.OrderBy(u => u.Username),
                    "rolename" => order.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.Role.Name)
                        : query.OrderBy(u => u.Role.Name),
                    "createdat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderBy(u => u.Username)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserReadDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        RoleName = u.Role.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список пользователей. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    users.Count, totalCount, page);

                return Ok(new
                {
                    data = users,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return StatusCode(500, "Произошла ошибка при получении списка пользователей.");
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с Id {UserId} не найден", id);
                    return NotFound();
                }

                var dto = new UserReadDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RoleName = user.Role.Name
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
                return StatusCode(500, "Произошла ошибка при получении пользователя.");
            }
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                // Проверка на существующего пользователя
                if (await _context.Users.AnyAsync(u => u.Username == createDto.Username && !u.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания пользователя с существующим именем: {Username}", createDto.Username);
                    return BadRequest("Пользователь с таким именем уже существует.");
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == createDto.RoleId && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка создания пользователя с несуществующей ролью: {RoleId}", createDto.RoleId);
                    return BadRequest("Роль не найдена.");
                }

                var passwordHash = _passwordService.HashPassword(createDto.Password);

                var user = new User
                {
                    Username = createDto.Username,
                    PasswordHash = passwordHash,
                    RoleId = createDto.RoleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {Username} создан администратором {AdminUsername}",
                    user.Username, username);

                var readDto = new UserReadDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RoleName = role.Name
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя {Username}", createDto.Username);
                return StatusCode(500, "Произошла ошибка при создании пользователя.");
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UserUpdateDto updateDto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (user == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего пользователя: {UserId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                if (!string.IsNullOrWhiteSpace(updateDto.Password))
                {
                    user.PasswordHash = _passwordService.HashPassword(updateDto.Password);
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == updateDto.RoleId && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка обновления пользователя {UserId} с несуществующей ролью: {RoleId}",
                        id, updateDto.RoleId);
                    return BadRequest("Роль не найдена.");
                }

                user.RoleId = updateDto.RoleId;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {UserId} обновлен администратором {AdminUsername}",
                    id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении пользователя.");
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (user == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего пользователя: {UserId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {UserId} ({Username}) удален администратором {AdminUsername}",
                    id, user.Username, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
                return StatusCode(500, "Произошла ошибка при удалении пользователя.");
            }
        }
    }
}
