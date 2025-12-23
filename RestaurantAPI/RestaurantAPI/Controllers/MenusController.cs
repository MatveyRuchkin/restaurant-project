using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Helpers;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IBusinessValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<MenusController> _logger;

        public MenusController(
            RestaurantDbContext context,
            IBusinessValidationService validationService,
            IMapper mapper,
            ILogger<MenusController> logger)
        {
            _context = context;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Menus - доступно всем (включая неавторизованных)
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult<PagedResult<MenuReadDto>>> GetMenus(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Menus
                    .Where(m => !m.IsDeleted)
                    .AsQueryable();

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
                    .ToListAsync();

                var menuDtos = _mapper.Map<IEnumerable<MenuReadDto>>(menus);

                _logger.LogInformation(
                    "Получен список меню. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    menus.Count, totalCount, page);

                return Ok(new PagedResult<MenuReadDto>
                {
                    Data = menuDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка меню");
                throw;
            }
        }

        // GET: api/Menus/5 - доступно всем (включая неавторизованных)
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuReadDto>> GetMenu(Guid id)
        {
            try
            {
                var menu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (menu == null)
                {
                    _logger.LogWarning("Меню с Id {MenuId} не найдено", id);
                    throw new NotFoundException("Меню не найдено");
                }

                var dto = _mapper.Map<MenuReadDto>(menu);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении меню {MenuId}", id);
                throw;
            }
        }

        // POST: api/Menus - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<MenuReadDto>> CreateMenu(MenuCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Menus.AnyAsync(m => m.Name == createDto.Name && !m.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания меню с существующим названием: {Name}", createDto.Name);
                    throw new BadRequestException("Меню с таким названием уже существует.");
                }

                var menu = _mapper.Map<Menu>(createDto);
                menu.CreatedAt = DateTime.UtcNow;
                menu.CreatedBy = username;

                await _context.Menus.AddAsync(menu);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {Name} создано администратором {Username}", menu.Name, username);

                var readDto = _mapper.Map<MenuReadDto>(menu);
                return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании меню {Name}", createDto.Name);
                throw;
            }
        }

        // PUT: api/Menus/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateMenu(Guid id, MenuUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var menu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего меню: {MenuId}", id);
                    throw new NotFoundException("Меню не найдено");
                }

                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Menus.AnyAsync(m => m.Name == updateDto.Name && m.Id != id && !m.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления меню {MenuId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    throw new BadRequestException("Меню с таким названием уже существует.");
                }

                _mapper.Map(updateDto, menu);
                menu.UpdatedAt = DateTime.UtcNow;
                menu.UpdatedBy = username;

                _context.Menus.Update(menu);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {MenuId} обновлено администратором {Username}", id, username);

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
                _logger.LogError(ex, "Ошибка при обновлении меню {MenuId}", id);
                throw;
            }
        }

        // DELETE: api/Menus/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            try
            {
                var menu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего меню: {MenuId}", id);
                    throw new NotFoundException("Меню не найдено");
                }

                // Валидация бизнес-логики удаления
                await _validationService.ValidateMenuDeletionAsync(id);

                var username = GetCurrentUsername();

                menu.IsDeleted = true;
                menu.DeletedAt = DateTime.UtcNow;
                menu.DeletedBy = username;

                _context.Menus.Update(menu);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Меню {MenuId} ({Name}) удалено администратором {Username}",
                    id, menu.Name, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении меню {MenuId}", id);
                throw;
            }
        }
    }
}
