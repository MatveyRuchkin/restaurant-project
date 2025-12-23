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
    public class DishesController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IBusinessValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<DishesController> _logger;

        public DishesController(
            RestaurantDbContext context,
            IBusinessValidationService validationService,
            IMapper mapper,
            ILogger<DishesController> logger)
        {
            _context = context;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Получение списка блюд с фильтрацией, сортировкой и пагинацией
        /// Доступно всем (включая неавторизованных пользователей)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<DishReadDto>>> GetDishes(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                IQueryable<Dish> query = _context.Dishes
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Category);

                if (categoryId.HasValue)
                {
                    query = query.Where(d => d.CategoryId == categoryId.Value);
                }

                if (minPrice.HasValue)
                {
                    query = query.Where(d => d.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(d => d.Price <= maxPrice.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || 
                                           (d.Description != null && d.Description.Contains(search)));
                }

                var orderedQuery = sortBy.ToLower() switch
                {
                    "price" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Price)
                        : query.OrderBy(d => d.Price),
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Name)
                        : query.OrderBy(d => d.Name),
                    "category" => order.ToLower() == "desc"
                        ? query.OrderByDescending(d => d.Category.Name)
                        : query.OrderBy(d => d.Category.Name),
                    _ => query.OrderBy(d => d.Name)
                };

                var totalCount = await orderedQuery.CountAsync();
                var dishes = await orderedQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dishDtos = _mapper.Map<IEnumerable<DishReadDto>>(dishes);

                _logger.LogInformation(
                    "Получен список блюд. Количество: {Count}, Всего: {Total}, Страница: {Page}, Размер страницы: {PageSize}",
                    dishes.Count, totalCount, page, pageSize);

                return Ok(new PagedResult<DishReadDto>
                {
                    Data = dishDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка блюд");
                throw;
            }
        }

        /// <summary>
        /// Получение блюда по ID. Доступно всем (включая неавторизованных пользователей)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DishReadDto>> GetDish(Guid id)
        {
            try
            {
                var dish = await _context.Dishes
                    .Include(d => d.Category)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dish == null)
                {
                    _logger.LogWarning("Блюдо с Id {DishId} не найдено", id);
                    throw new NotFoundException("Блюдо не найдено");
                }

                var dto = _mapper.Map<DishReadDto>(dish);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении блюда {DishId}", id);
                throw;
            }
        }

        /// <summary>
        /// Создание нового блюда (только для админов)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<DishReadDto>> CreateDish(DishCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                if (await _context.Dishes.AnyAsync(d => d.Name == createDto.Name && !d.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания блюда с существующим названием: {Name}", createDto.Name);
                    throw new BadRequestException("Блюдо с таким названием уже существует.");
                }

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка создания блюда с несуществующей категорией: {CategoryId}", createDto.CategoryId);
                    throw new BadRequestException("Категория не найдена.");
                }

                var dish = _mapper.Map<Dish>(createDto);
                dish.CreatedAt = DateTime.UtcNow;
                dish.CreatedBy = username;

                await _context.Dishes.AddAsync(dish);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {Name} создано администратором {Username}", dish.Name, username);

                var readDto = _mapper.Map<DishReadDto>(dish);
                readDto.CategoryName = category.Name;

                return CreatedAtAction(nameof(GetDish), new { id = dish.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании блюда {Name}", createDto.Name);
                throw;
            }
        }

        /// <summary>
        /// Обновление блюда (только для админов)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateDish(Guid id, DishUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var dish = await _context.Dishes
                    .Include(d => d.Category)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего блюда: {DishId}", id);
                    throw new NotFoundException("Блюдо не найдено");
                }

                var username = GetCurrentUsername();

                if (await _context.Dishes.AnyAsync(d => d.Name == updateDto.Name && d.Id != id && !d.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления блюда {DishId} с существующим названием: {Name}", 
                        id, updateDto.Name);
                    throw new BadRequestException("Блюдо с таким названием уже существует.");
                }

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка обновления блюда {DishId} с несуществующей категорией: {CategoryId}", 
                        id, updateDto.CategoryId);
                    throw new BadRequestException("Категория не найдена.");
                }

                _mapper.Map(updateDto, dish);
                dish.UpdatedAt = DateTime.UtcNow;
                dish.UpdatedBy = username;

                _context.Dishes.Update(dish);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishId} обновлено администратором {Username}", id, username);

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
                _logger.LogError(ex, "Ошибка при обновлении блюда {DishId}", id);
                throw;
            }
        }

        /// <summary>
        /// Мягкое удаление блюда (только для админов)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteDish(Guid id)
        {
            try
            {
                var dish = await _context.Dishes
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего блюда: {DishId}", id);
                    throw new NotFoundException("Блюдо не найдено");
                }

                await _validationService.ValidateDishDeletionAsync(id);

                var username = GetCurrentUsername();

                dish.IsDeleted = true;
                dish.DeletedAt = DateTime.UtcNow;
                dish.DeletedBy = username;

                _context.Dishes.Update(dish);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Блюдо {DishId} ({Name}) удалено администратором {Username}", 
                    id, dish.Name, username);

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
                _logger.LogError(ex, "Ошибка при удалении блюда {DishId}", id);
                throw;
            }
        }
    }
}
