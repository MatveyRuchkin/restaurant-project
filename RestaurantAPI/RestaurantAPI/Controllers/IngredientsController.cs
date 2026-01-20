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
    public class IngredientsController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IBusinessValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(
            RestaurantDbContext context,
            IBusinessValidationService validationService,
            IMapper mapper,
            ILogger<IngredientsController> logger)
        {
            _context = context;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Ingredients - доступно всем авторизованным
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResult<IngredientReadDto>>> GetIngredients(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Ingredients
                    .Where(i => !i.IsDeleted)
                    .AsQueryable();

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
                    .ToListAsync();

                var ingredientDtos = _mapper.Map<IEnumerable<IngredientReadDto>>(ingredients);

                _logger.LogInformation(
                    "Получен список ингредиентов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    ingredients.Count, totalCount, page);

                return Ok(new PagedResult<IngredientReadDto>
                {
                    Data = ingredientDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ингредиентов");
                throw;
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
                    throw new NotFoundException("Ингредиент не найден");
                }

                var dto = _mapper.Map<IngredientReadDto>(ingredient);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении ингредиента {IngredientId}", id);
                throw;
            }
        }

        // POST: api/Ingredients - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<IngredientReadDto>> CreateIngredient(IngredientCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Ingredients.AnyAsync(i => i.Name == createDto.Name && !i.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания ингредиента с существующим названием: {Name}", createDto.Name);
                    throw new BadRequestException("Ингредиент с таким названием уже существует.");
                }

                var ingredient = _mapper.Map<Ingredient>(createDto);
                ingredient.CreatedAt = DateTime.UtcNow;
                ingredient.CreatedBy = username;

                await _context.Ingredients.AddAsync(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {Name} создан администратором {Username}",
                    ingredient.Name, username);

                var readDto = _mapper.Map<IngredientReadDto>(ingredient);
                return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании ингредиента {Name}", createDto.Name);
                throw;
            }
        }

        // PUT: api/Ingredients/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateIngredient(Guid id, IngredientUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (ingredient == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего ингредиента: {IngredientId}", id);
                    throw new NotFoundException("Ингредиент не найден");
                }

                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Ingredients.AnyAsync(i => i.Name == updateDto.Name && i.Id != id && !i.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления ингредиента {IngredientId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    throw new BadRequestException("Ингредиент с таким названием уже существует.");
                }

                _mapper.Map(updateDto, ingredient);
                ingredient.UpdatedAt = DateTime.UtcNow;
                ingredient.UpdatedBy = username;

                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientId} обновлен администратором {Username}", id, username);

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
                _logger.LogError(ex, "Ошибка при обновлении ингредиента {IngredientId}", id);
                throw;
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
                    throw new NotFoundException("Ингредиент не найден");
                }

                // Валидация бизнес-логики удаления
                await _validationService.ValidateIngredientDeletionAsync(id);

                var username = GetCurrentUsername();

                ingredient.IsDeleted = true;
                ingredient.DeletedAt = DateTime.UtcNow;
                ingredient.DeletedBy = username;

                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ингредиент {IngredientId} ({Name}) удален администратором {Username}",
                    id, ingredient.Name, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении ингредиента {IngredientId}", id);
                throw;
            }
        }
    }
}
