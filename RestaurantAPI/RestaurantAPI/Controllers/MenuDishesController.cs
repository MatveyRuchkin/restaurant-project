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
    public class MenuDishesController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<MenuDishesController> _logger;

        public MenuDishesController(RestaurantDbContext context, ILogger<MenuDishesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/MenuDishes - доступно всем (включая неавторизованных)
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult> GetMenuDishes(
            Guid? menuId = null,
            Guid? dishId = null,
            string sortBy = "menuname",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.MenuDishes
                    .Include(md => md.Menu)
                    .Include(md => md.Dish)
                    .Where(md => !md.IsDeleted);

                // Фильтрация по меню
                if (menuId.HasValue)
                {
                    query = query.Where(md => md.MenuId == menuId.Value);
                }

                // Фильтрация по блюду
                if (dishId.HasValue)
                {
                    query = query.Where(md => md.DishId == dishId.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "menuname" => order.ToLower() == "desc"
                        ? query.OrderByDescending(md => md.Menu.Name)
                        : query.OrderBy(md => md.Menu.Name),
                    "dishname" => order.ToLower() == "desc"
                        ? query.OrderByDescending(md => md.Dish.Name)
                        : query.OrderBy(md => md.Dish.Name),
                    _ => query.OrderBy(md => md.Menu.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var menuDishes = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(md => new MenuDishReadDto
                    {
                        Id = md.Id,
                        MenuId = md.MenuId,
                        MenuName = md.Menu.Name,
                        DishId = md.DishId,
                        DishName = md.Dish.Name
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список блюд в меню. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    menuDishes.Count, totalCount, page);

                return Ok(new PagedResult<MenuDishReadDto>
                {
                    Data = menuDishes,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка блюд в меню");
                throw;
            }
        }

        // GET: api/MenuDishes/5 - доступно всем (включая неавторизованных)
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuDishReadDto>> GetMenuDish(Guid id)
        {
            try
            {
                var menuDish = await _context.MenuDishes
                    .Include(md => md.Menu)
                    .Include(md => md.Dish)
                    .FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);

                if (menuDish == null)
                {
                    _logger.LogWarning("Блюдо в меню с Id {MenuDishId} не найдено", id);
                    throw new NotFoundException("Блюдо в меню не найдено");
                }

                var dto = new MenuDishReadDto
                {
                    Id = menuDish.Id,
                    MenuId = menuDish.MenuId,
                    MenuName = menuDish.Menu.Name,
                    DishId = menuDish.DishId,
                    DishName = menuDish.Dish.Name
                };

                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении блюда в меню {MenuDishId}", id);
                throw;
            }
        }

        // POST: api/MenuDishes - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<MenuDishReadDto>> CreateMenuDish(MenuDishCreateDto createDto)
        {
            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.MenuDishes.AnyAsync(md =>
                    md.MenuId == createDto.MenuId &&
                    md.DishId == createDto.DishId &&
                    !md.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания дубликата блюда в меню. MenuId: {MenuId}, DishId: {DishId}",
                        createDto.MenuId, createDto.DishId);
                    throw new BadRequestException("Это блюдо уже добавлено в данное меню");
                }

                var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == createDto.MenuId && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка создания блюда в меню с несуществующим меню: {MenuId}", createDto.MenuId);
                    throw new NotFoundException("Меню не найдено");
                }

                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка создания блюда в меню с несуществующим блюдом: {DishId}", createDto.DishId);
                    throw new NotFoundException("Блюдо не найдено");
                }

                var menuDish = new MenuDish
                {
                    MenuId = createDto.MenuId,
                    DishId = createDto.DishId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.MenuDishes.Add(menuDish);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishName} добавлено в меню {MenuName} администратором {Username}",
                    dish.Name, menu.Name, username);

                var readDto = new MenuDishReadDto
                {
                    Id = menuDish.Id,
                    MenuId = menuDish.MenuId,
                    MenuName = menu.Name,
                    DishId = menuDish.DishId,
                    DishName = dish.Name
                };

                return CreatedAtAction(nameof(GetMenuDish), new { id = menuDish.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании блюда в меню");
                throw;
            }
        }

        // PUT: api/MenuDishes/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateMenuDish(Guid id, MenuDishUpdateDto updateDto)
        {
            try
            {
                var menuDish = await _context.MenuDishes.FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);
                if (menuDish == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего блюда в меню: {MenuDishId}", id);
                    throw new NotFoundException("Блюдо в меню не найдено");
                }

                var username = GetCurrentUsername();

                var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == updateDto.MenuId && !m.IsDeleted);
                if (menu == null)
                {
                    _logger.LogWarning("Попытка обновления блюда в меню {MenuDishId} с несуществующим меню: {MenuId}",
                        id, updateDto.MenuId);
                    throw new NotFoundException("Меню не найдено");
                }

                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == updateDto.DishId && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка обновления блюда в меню {MenuDishId} с несуществующим блюдом: {DishId}",
                        id, updateDto.DishId);
                    throw new NotFoundException("Блюдо не найдено");
                }

                menuDish.MenuId = updateDto.MenuId;
                menuDish.DishId = updateDto.DishId;
                menuDish.UpdatedAt = DateTime.UtcNow;
                menuDish.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо в меню {MenuDishId} обновлено администратором {Username}", id, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении блюда в меню {MenuDishId}", id);
                throw;
            }
        }

        // DELETE: api/MenuDishes/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteMenuDish(Guid id)
        {
            try
            {
                var menuDish = await _context.MenuDishes
                    .Include(md => md.Menu)
                    .Include(md => md.Dish)
                    .FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);
                if (menuDish == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего блюда в меню: {MenuDishId}", id);
                    throw new NotFoundException("Блюдо в меню не найдено");
                }

                var username = GetCurrentUsername();

                menuDish.IsDeleted = true;
                menuDish.DeletedAt = DateTime.UtcNow;
                menuDish.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishName} удалено из меню {MenuName} администратором {Username}",
                    menuDish.Dish.Name, menuDish.Menu.Name, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении блюда в меню {MenuDishId}", id);
                throw;
            }
        }
    }
}