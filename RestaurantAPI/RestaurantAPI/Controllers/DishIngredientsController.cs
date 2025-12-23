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
    public class DishIngredientsController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<DishIngredientsController> _logger;

        public DishIngredientsController(RestaurantDbContext context, ILogger<DishIngredientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/DishIngredients - доступно всем (включая неавторизованных)
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult> GetDishIngredients(
            Guid? dishId = null,
            Guid? ingredientId = null,
            string sortBy = "dishname",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.DishIngredients
                    .Include(di => di.Dish)
                    .Include(di => di.Ingredient)
                    .Where(di => !di.IsDeleted);

                // Фильтрация по блюду
                if (dishId.HasValue)
                {
                    query = query.Where(di => di.DishId == dishId.Value);
                }

                // Фильтрация по ингредиенту
                if (ingredientId.HasValue)
                {
                    query = query.Where(di => di.IngredientId == ingredientId.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "dishname" => order.ToLower() == "desc"
                        ? query.OrderByDescending(di => di.Dish.Name)
                        : query.OrderBy(di => di.Dish.Name),
                    "ingredientname" => order.ToLower() == "desc"
                        ? query.OrderByDescending(di => di.Ingredient.Name)
                        : query.OrderBy(di => di.Ingredient.Name),
                    "quantity" => order.ToLower() == "desc"
                        ? query.OrderByDescending(di => di.Quantity)
                        : query.OrderBy(di => di.Quantity),
                    _ => query.OrderBy(di => di.Dish.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var dishIngredients = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(di => new DishIngredientReadDto
                    {
                        Id = di.Id,
                        DishId = di.DishId,
                        DishName = di.Dish.Name,
                        IngredientId = di.IngredientId,
                        IngredientName = di.Ingredient.Name,
                        Quantity = di.Quantity
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список ингредиентов блюд. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    dishIngredients.Count, totalCount, page);

                return Ok(new PagedResult<DishIngredientReadDto>
                {
                    Data = dishIngredients,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ингредиентов блюд");
                throw;
            }
        }

        // GET: api/DishIngredients/5 - доступно всем (включая неавторизованных)
        [HttpGet("{id}")]
        public async Task<ActionResult<DishIngredientReadDto>> GetDishIngredient(Guid id)
        {
            try
            {
                var di = await _context.DishIngredients
                    .Include(x => x.Dish)
                    .Include(x => x.Ingredient)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (di == null)
                {
                    _logger.LogWarning("Ингредиент блюда с Id {DishIngredientId} не найден", id);
                    throw new NotFoundException("Ингредиент блюда не найден");
                }

                var dto = new DishIngredientReadDto
                {
                    Id = di.Id,
                    DishId = di.DishId,
                    DishName = di.Dish.Name,
                    IngredientId = di.IngredientId,
                    IngredientName = di.Ingredient.Name,
                    Quantity = di.Quantity
                };

                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении ингредиента блюда {DishIngredientId}", id);
                throw;
            }
        }

        // POST: api/DishIngredients - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<DishIngredientReadDto>> CreateDishIngredient(DishIngredientCreateDto createDto)
        {
            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.DishIngredients.AnyAsync(di =>
                    di.DishId == createDto.DishId &&
                    di.IngredientId == createDto.IngredientId &&
                    !di.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания дубликата ингредиента блюда. DishId: {DishId}, IngredientId: {IngredientId}",
                        createDto.DishId, createDto.IngredientId);
                    throw new BadRequestException("Этот ингредиент уже добавлен к данному блюду");
                }

                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка создания ингредиента блюда с несуществующим блюдом: {DishId}", createDto.DishId);
                    throw new NotFoundException("Блюдо не найдено");
                }

                var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == createDto.IngredientId && !i.IsDeleted);
                if (ingredient == null)
                {
                    _logger.LogWarning("Попытка создания ингредиента блюда с несуществующим ингредиентом: {IngredientId}", createDto.IngredientId);
                    throw new NotFoundException("Ингредиент не найден");
                }

                var dishIngredient = new DishIngredient
                {
                    DishId = createDto.DishId,
                    IngredientId = createDto.IngredientId,
                    Quantity = createDto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.DishIngredients.Add(dishIngredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientName} добавлен к блюду {DishName} администратором {Username}",
                    ingredient.Name, dish.Name, username);

                var readDto = new DishIngredientReadDto
                {
                    Id = dishIngredient.Id,
                    DishId = dish.Id,
                    DishName = dish.Name,
                    IngredientId = ingredient.Id,
                    IngredientName = ingredient.Name,
                    Quantity = dishIngredient.Quantity
                };

                return CreatedAtAction(nameof(GetDishIngredient), new { id = dishIngredient.Id }, readDto);
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
                _logger.LogError(ex, "Ошибка при создании ингредиента блюда");
                throw;
            }
        }

        // PUT: api/DishIngredients/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateDishIngredient(Guid id, DishIngredientUpdateDto updateDto)
        {
            try
            {
                var dishIngredient = await _context.DishIngredients
                    .Include(di => di.Ingredient)
                    .Include(di => di.Dish)
                    .FirstOrDefaultAsync(di => di.Id == id && !di.IsDeleted);

                if (dishIngredient == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего ингредиента блюда: {DishIngredientId}", id);
                    throw new NotFoundException("Ингредиент блюда не найден");
                }

                var username = GetCurrentUsername();

                var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == updateDto.IngredientId && !i.IsDeleted);
                if (ingredient == null)
                {
                    _logger.LogWarning("Попытка обновления ингредиента блюда {DishIngredientId} с несуществующим ингредиентом: {IngredientId}",
                        id, updateDto.IngredientId);
                    throw new NotFoundException("Ингредиент не найден");
                }

                dishIngredient.IngredientId = updateDto.IngredientId;
                dishIngredient.Quantity = updateDto.Quantity;
                dishIngredient.UpdatedAt = DateTime.UtcNow;
                dishIngredient.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент блюда {DishIngredientId} обновлен администратором {Username}", id, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении ингредиента блюда {DishIngredientId}", id);
                throw;
            }
        }

        // DELETE: api/DishIngredients/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteDishIngredient(Guid id)
        {
            try
            {
                var dishIngredient = await _context.DishIngredients
                    .Include(di => di.Ingredient)
                    .Include(di => di.Dish)
                    .FirstOrDefaultAsync(di => di.Id == id && !di.IsDeleted);

                if (dishIngredient == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего ингредиента блюда: {DishIngredientId}", id);
                    throw new NotFoundException("Ингредиент блюда не найден");
                }

                var username = GetCurrentUsername();

                dishIngredient.IsDeleted = true;
                dishIngredient.DeletedAt = DateTime.UtcNow;
                dishIngredient.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientName} удален из блюда {DishName} администратором {Username}",
                    dishIngredient.Ingredient.Name, dishIngredient.Dish.Name, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении ингредиента блюда {DishIngredientId}", id);
                throw;
            }
        }
    }
}
