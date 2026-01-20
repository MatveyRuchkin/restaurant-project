using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Helpers;
using RestaurantAPI.Domain.Entities;
using RestaurantAPI.Application.Interfaces;
using RestaurantAPI.Infrastructure.Persistence;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class UsersController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            RestaurantDbContext context,
            IPasswordService passwordService,
            IMapper mapper,
            ILogger<UsersController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Users
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserReadDto>>> GetUsers(
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
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.Role)
                    .AsQueryable();

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
                var orderedQuery = sortBy.ToLower() switch
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
                var totalCount = await orderedQuery.CountAsync();

                // Пагинация
                var users = await orderedQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = _mapper.Map<IEnumerable<UserReadDto>>(users);

                _logger.LogInformation(
                    "Получен список пользователей. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    users.Count, totalCount, page);

                return Ok(new PagedResult<UserReadDto>
                {
                    Data = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                throw;
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
                    throw new NotFoundException("Пользователь не найден");
                }

                var dto = _mapper.Map<UserReadDto>(user);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
                throw;
            }
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                // Проверка на существующего пользователя
                if (await _context.Users.AnyAsync(u => u.Username == createDto.Username && !u.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания пользователя с существующим именем: {Username}", createDto.Username);
                    throw new BadRequestException("Пользователь с таким именем уже существует.");
                }

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == createDto.RoleId && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка создания пользователя с несуществующей ролью: {RoleId}", createDto.RoleId);
                    throw new BadRequestException("Роль не найдена.");
                }

                var passwordHash = _passwordService.HashPassword(createDto.Password);

                var user = _mapper.Map<User>(createDto);
                user.PasswordHash = passwordHash;
                user.CreatedAt = DateTime.UtcNow;
                user.CreatedBy = username;

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {Username} создан администратором {AdminUsername}",
                    user.Username, username);

                var readDto = _mapper.Map<UserReadDto>(user);
                readDto.RoleName = role.Name;

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя {Username}", createDto.Username);
                throw;
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UserUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (user == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего пользователя: {UserId}", id);
                    throw new NotFoundException("Пользователь не найден");
                }

                var username = GetCurrentUsername();

                if (!string.IsNullOrWhiteSpace(updateDto.Password))
                {
                    user.PasswordHash = _passwordService.HashPassword(updateDto.Password);
                }

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == updateDto.RoleId && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка обновления пользователя {UserId} с несуществующей ролью: {RoleId}",
                        id, updateDto.RoleId);
                    throw new BadRequestException("Роль не найдена.");
                }

                user.RoleId = updateDto.RoleId;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = username;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {UserId} обновлен администратором {AdminUsername}",
                    id, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
                throw;
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                if (user == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего пользователя: {UserId}", id);
                    throw new NotFoundException("Пользователь не найден");
                }

                var username = GetCurrentUsername();

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.DeletedBy = username;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {UserId} ({Username}) удален администратором {AdminUsername}",
                    id, user.Username, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
                throw;
            }
        }
    }
}
