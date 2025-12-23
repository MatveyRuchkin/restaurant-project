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
    public class CategoriesController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IBusinessValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            RestaurantDbContext context,
            IBusinessValidationService validationService,
            IMapper mapper,
            ILogger<CategoriesController> logger)
        {
            _context = context;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Categories - доступно всем (включая неавторизованных)
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult<PagedResult<CategoryReadDto>>> GetCategories(
            string? search = null,
            string sortBy = "name",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Categories
                    .Where(c => !c.IsDeleted)
                    .AsQueryable();

                // Поиск по названию
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.Name.Contains(search) ||
                                           (c.Notes != null && c.Notes.Contains(search)));
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "name" => order.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.Name)
                        : query.OrderBy(c => c.Name),
                    _ => query.OrderBy(c => c.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var categories = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var categoryDtos = _mapper.Map<IEnumerable<CategoryReadDto>>(categories);

                _logger.LogInformation(
                    "Получен список категорий. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    categories.Count, totalCount, page);

                return Ok(new PagedResult<CategoryReadDto>
                {
                    Data = categoryDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка категорий");
                throw;
            }
        }

        // GET: api/Categories/5 - доступно всем (включая неавторизованных)
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryReadDto>> GetCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Категория с Id {CategoryId} не найдена", id);
                    throw new NotFoundException("Категория не найдена");
                }

                var dto = _mapper.Map<CategoryReadDto>(category);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении категории {CategoryId}", id);
                throw;
            }
        }

        // POST: api/Categories - только админ
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<CategoryReadDto>> CreateCategory(CategoryCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Categories.AnyAsync(c => c.Name == createDto.Name && !c.IsDeleted))
                {
                    _logger.LogWarning("Попытка создания категории с существующим названием: {Name}", createDto.Name);
                    throw new BadRequestException("Категория с таким названием уже существует.");
                }

                var category = _mapper.Map<Category>(createDto);
                category.CreatedAt = DateTime.UtcNow;
                category.CreatedBy = username;

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {Name} создана администратором {Username}", category.Name, username);

                var readDto = _mapper.Map<CategoryReadDto>(category);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, readDto);
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании категории {Name}", createDto.Name);
                throw;
            }
        }

        // PUT: api/Categories/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, CategoryUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующей категории: {CategoryId}", id);
                    throw new NotFoundException("Категория не найдена");
                }

                var username = GetCurrentUsername();

                // Проверка на дубликат
                if (await _context.Categories.AnyAsync(c => c.Name == updateDto.Name && c.Id != id && !c.IsDeleted))
                {
                    _logger.LogWarning("Попытка обновления категории {CategoryId} с существующим названием: {Name}",
                        id, updateDto.Name);
                    throw new BadRequestException("Категория с таким названием уже существует.");
                }

                _mapper.Map(updateDto, category);
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = username;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {CategoryId} обновлена администратором {Username}", id, username);

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
                _logger.LogError(ex, "Ошибка при обновлении категории {CategoryId}", id);
                throw;
            }
        }

        // DELETE: api/Categories/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующей категории: {CategoryId}", id);
                    throw new NotFoundException("Категория не найдена");
                }

                // Валидация бизнес-логики удаления
                await _validationService.ValidateCategoryDeletionAsync(id);

                var username = GetCurrentUsername();

                category.IsDeleted = true;
                category.DeletedAt = DateTime.UtcNow;
                category.DeletedBy = username;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Категория {CategoryId} ({Name}) удалена администратором {Username}",
                    id, category.Name, username);

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
                _logger.LogError(ex, "Ошибка при удалении категории {CategoryId}", id);
                throw;
            }
        }
    }
}
