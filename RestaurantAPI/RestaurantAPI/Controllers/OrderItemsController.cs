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
    public class OrderItemsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<OrderItemsController> _logger;

        public OrderItemsController(RestaurantDbContext context, ILogger<OrderItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/OrderItems - официанты и админы
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize(Policy = "Waiter")]
        public async Task<ActionResult> GetOrderItems(
            Guid? orderId = null,
            Guid? dishId = null,
            string sortBy = "dishname",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.OrderItems
                    .Include(oi => oi.Dish)
                    .Where(oi => !oi.IsDeleted);

                // Фильтрация по заказу
                if (orderId.HasValue)
                {
                    query = query.Where(oi => oi.OrderId == orderId.Value);
                }

                // Фильтрация по блюду
                if (dishId.HasValue)
                {
                    query = query.Where(oi => oi.DishId == dishId.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "dishname" => order.ToLower() == "desc"
                        ? query.OrderByDescending(oi => oi.Dish.Name)
                        : query.OrderBy(oi => oi.Dish.Name),
                    "quantity" => order.ToLower() == "desc"
                        ? query.OrderByDescending(oi => oi.Quantity)
                        : query.OrderBy(oi => oi.Quantity),
                    "price" => order.ToLower() == "desc"
                        ? query.OrderByDescending(oi => oi.Price)
                        : query.OrderBy(oi => oi.Price),
                    _ => query.OrderBy(oi => oi.Dish.Name)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(oi => new OrderItemReadDto
                    {
                        Id = oi.Id,
                        DishId = oi.DishId,
                        DishName = oi.Dish.Name,
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список элементов заказов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    items.Count, totalCount, page);

                return Ok(new
                {
                    data = items,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка элементов заказов");
                return StatusCode(500, "Произошла ошибка при получении списка элементов заказов.");
            }
        }

        // GET: api/OrderItems/5 - официанты и админы
        [HttpGet("{id}")]
        [Authorize(Policy = "Waiter")]
        public async Task<ActionResult<OrderItemReadDto>> GetOrderItem(Guid id)
        {
            try
            {
                var item = await _context.OrderItems
                    .Include(oi => oi.Dish)
                    .FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);

                if (item == null)
                {
                    _logger.LogWarning("Элемент заказа с Id {OrderItemId} не найден", id);
                    return NotFound();
                }

                var dto = new OrderItemReadDto
                {
                    Id = item.Id,
                    DishId = item.DishId,
                    DishName = item.Dish.Name,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении элемента заказа {OrderItemId}", id);
                return StatusCode(500, "Произошла ошибка при получении элемента заказа.");
            }
        }

        // POST: api/OrderItems - только админ (обычно создаются через Orders)
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<OrderItemReadDto>> CreateOrderItem(OrderItemCreateDto createDto)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";

                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId && !d.IsDeleted);
                if (dish == null)
                {
                    _logger.LogWarning("Попытка создания элемента заказа с несуществующим блюдом: {DishId}", createDto.DishId);
                    return BadRequest("Блюдо не найдено.");
                }

                if (createDto.Quantity <= 0)
                {
                    _logger.LogWarning("Попытка создания элемента заказа с неверным количеством: {Quantity}", createDto.Quantity);
                    return BadRequest("Количество должно быть больше 0.");
                }

                var orderItem = new OrderItem
                {
                    DishId = createDto.DishId,
                    Quantity = createDto.Quantity,
                    Price = dish.Price * createDto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Элемент заказа создан администратором {Username}. Блюдо: {DishName}, Количество: {Quantity}",
                    username, dish.Name, createDto.Quantity);

                var readDto = new OrderItemReadDto
                {
                    Id = orderItem.Id,
                    DishId = orderItem.DishId,
                    DishName = dish.Name,
                    Quantity = orderItem.Quantity,
                    Price = orderItem.Price
                };

                return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании элемента заказа");
                return StatusCode(500, "Произошла ошибка при создании элемента заказа.");
            }
        }

        // PUT: api/OrderItems/5 - только админ
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateOrderItem(Guid id, OrderItemUpdateDto updateDto)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Dish)
                    .FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);

                if (orderItem == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего элемента заказа: {OrderItemId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                if (updateDto.Quantity <= 0)
                {
                    _logger.LogWarning("Попытка обновления элемента заказа {OrderItemId} с неверным количеством: {Quantity}",
                        id, updateDto.Quantity);
                    return BadRequest("Количество должно быть больше 0.");
                }

                orderItem.Quantity = updateDto.Quantity;
                orderItem.Price = orderItem.Dish.Price * updateDto.Quantity;
                orderItem.UpdatedAt = DateTime.UtcNow;
                orderItem.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Элемент заказа {OrderItemId} обновлен администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении элемента заказа {OrderItemId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении элемента заказа.");
            }
        }

        // DELETE: api/OrderItems/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteOrderItem(Guid id)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Dish)
                    .FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);
                if (orderItem == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего элемента заказа: {OrderItemId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                orderItem.IsDeleted = true;
                orderItem.DeletedAt = DateTime.UtcNow;
                orderItem.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Элемент заказа {OrderItemId} (Блюдо: {DishName}) удален администратором {Username}",
                    id, orderItem.Dish.Name, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении элемента заказа {OrderItemId}", id);
                return StatusCode(500, "Произошла ошибка при удалении элемента заказа.");
            }
        }
    }
}
