using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Helpers;
using RestaurantAPI.Models;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class RolesController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            RestaurantDbContext context,
            IMapper mapper,
            ILogger<RolesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Roles
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult<PagedResult<RoleReadDto>>> GetRoles(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Roles
                    .Where(r => !r.IsDeleted)
                    .AsQueryable();

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
                var totalCount = query.Count();

                // Пагинация
                var roles = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var roleDtos = _mapper.Map<IEnumerable<RoleReadDto>>(roles);

                _logger.LogInformation(
                    "Получен список ролей. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    roles.Count, totalCount, page);

                return Ok(new PagedResult<RoleReadDto>
                {
                    Data = roleDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ролей");
                throw;
            }
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(Guid id)
        {
            try
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Роль с Id {RoleId} не найдена", id);
                    throw new NotFoundException("Роль не найдена");
                }

                var dto = _mapper.Map<RoleReadDto>(role);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении роли {RoleId}", id);
                throw;
            }
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole(RoleCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == createDto.Name && !r.IsDeleted);
                if (existingRole != null && !existingRole.IsDeleted)
                {
                    _logger.LogWarning("Попытка создания роли с существующим названием: {Name}", createDto.Name);
                    throw new BadRequestException("Роль с таким названием уже существует.");
                }

                var role = _mapper.Map<Role>(createDto);
                role.CreatedAt = DateTime.UtcNow;
                role.CreatedBy = username;

                await _context.Roles.AddAsync(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {Name} создана администратором {Username}", role.Name, username);

                var readDto = _mapper.Map<RoleReadDto>(role);
                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании роли {Name}", createDto.Name);
                throw;
            }
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, RoleUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующей роли: {RoleId}", id);
                    throw new NotFoundException("Роль не найдена");
                }

                var username = GetCurrentUsername();

                // Проверка на дубликат
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == updateDto.Name && r.Id != id && !r.IsDeleted);
                if (existingRole != null && existingRole.Id != id && !existingRole.IsDeleted)
                {
                    _logger.LogWarning("Попытка обновления роли {RoleId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    throw new BadRequestException("Роль с таким названием уже существует.");
                }

                _mapper.Map(updateDto, role);
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = username;

                _context.Roles.Update(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {RoleId} обновлена администратором {Username}", id, username);

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
                _logger.LogError(ex, "Ошибка при обновлении роли {RoleId}", id);
                throw;
            }
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующей роли: {RoleId}", id);
                    throw new NotFoundException("Роль не найдена");
                }

                var username = GetCurrentUsername();

                role.IsDeleted = true;
                role.DeletedAt = DateTime.UtcNow;
                role.DeletedBy = username;

                _context.Roles.Update(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Роль {RoleId} ({Name}) удалена администратором {Username}",
                    id, role.Name, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении роли {RoleId}", id);
                throw;
            }
        }
    }
}
