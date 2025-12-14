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
    public class OrdersController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(RestaurantDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Orders - официанты и админы видят все заказы
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        [Authorize(Policy = "Waiter")]
        public async Task<ActionResult> GetOrders(
            Guid? userId = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minTotal = null,
            decimal? maxTotal = null,
            string sortBy = "orderDate",
            string order = "desc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Where(o => !o.IsDeleted);

                // Фильтрация по пользователю
                if (userId.HasValue)
                {
                    query = query.Where(o => o.UserId == userId.Value);
                }

                // Фильтрация по статусу
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                // Фильтрация по дате
                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= endDate.Value);
                }

                // Фильтрация по сумме
                if (minTotal.HasValue)
                {
                    query = query.Where(o => o.Total >= minTotal.Value);
                }

                if (maxTotal.HasValue)
                {
                    query = query.Where(o => o.Total <= maxTotal.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "total" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.Total)
                        : query.OrderBy(o => o.Total),
                    "orderdate" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.OrderDate)
                        : query.OrderBy(o => o.OrderDate),
                    "status" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.Status)
                        : query.OrderBy(o => o.Status),
                    "username" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.User.Username)
                        : query.OrderBy(o => o.User.Username),
                    _ => query.OrderByDescending(o => o.OrderDate)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderReadDto
                    {
                        Id = o.Id,
                        Username = o.User.Username,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        Total = o.Total
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список заказов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    orders.Count, totalCount, page);

                return Ok(new
                {
                    data = orders,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка заказов");
                return StatusCode(500, "Произошла ошибка при получении списка заказов.");
            }
        }

        // GET: api/Orders/5 - пользователь видит только свои заказы, официант/админ - все
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderReadDto>> GetOrder(Guid id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

                if (order == null)
                {
                    _logger.LogWarning("Заказ с Id {OrderId} не найден", id);
                    return NotFound();
                }

                var userIdClaim = User.FindFirst("userId")?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin" && userRole != "Waiter" &&
                    userIdClaim != null && Guid.Parse(userIdClaim) != order.UserId)
                {
                    _logger.LogWarning("Попытка доступа к чужому заказу {OrderId} пользователем {UserId}",
                        id, userIdClaim);
                    return Forbid();
                }

                var dto = new OrderReadDto
                {
                    Id = order.Id,
                    Username = order.User.Username,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    Total = order.Total
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказа {OrderId}", id);
                return StatusCode(500, "Произошла ошибка при получении заказа.");
            }
        }

        // POST: api/Orders - любой авторизованный пользователь может создать заказ
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderReadDto>> CreateOrder(OrderCreateDto createDto)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin" && userRole != "Waiter" &&
                    userIdClaim != null && Guid.Parse(userIdClaim) != createDto.UserId)
                {
                    _logger.LogWarning("Попытка создания заказа для другого пользователя. UserId: {UserId}, RequestedUserId: {RequestedUserId}",
                        userIdClaim, createDto.UserId);
                    return Forbid();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == createDto.UserId && !u.IsDeleted);
                if (user == null)
                {
                    _logger.LogWarning("Попытка создания заказа для несуществующего пользователя: {UserId}", createDto.UserId);
                    return BadRequest("Пользователь не найден");
                }

                if (createDto.Items == null || createDto.Items.Count == 0)
                {
                    _logger.LogWarning("Попытка создания заказа без блюд для пользователя: {UserId}", createDto.UserId);
                    return BadRequest("Заказ должен содержать хотя бы одно блюдо");
                }

                var username = User?.Identity?.Name ?? "System";
                decimal total = 0;
                var order = new Order
                {
                    UserId = createDto.UserId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var itemDto in createDto.Items)
                {
                    var dish = await _context.Dishes
                        .FirstOrDefaultAsync(d => d.Id == itemDto.DishId && !d.IsDeleted);

                    if (dish == null)
                    {
                        _logger.LogWarning("Попытка добавить несуществующее блюдо {DishId} в заказ {OrderId}",
                            itemDto.DishId, order.Id);
                        return BadRequest($"Блюдо с ID {itemDto.DishId} не найдено или удалено");
                    }

                    if (itemDto.Quantity <= 0)
                    {
                        _logger.LogWarning("Попытка добавить блюдо {DishId} с неверным количеством {Quantity} в заказ {OrderId}",
                            itemDto.DishId, itemDto.Quantity, order.Id);
                        return BadRequest($"Количество блюда '{dish.Name}' должно быть больше 0");
                    }

                    if (itemDto.Quantity > 100)
                    {
                        _logger.LogWarning("Попытка добавить блюдо {DishId} с превышающим количеством {Quantity} в заказ {OrderId}",
                            itemDto.DishId, itemDto.Quantity, order.Id);
                        return BadRequest($"Количество блюда '{dish.Name}' не может превышать 100");
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        DishId = itemDto.DishId,
                        Quantity = itemDto.Quantity,
                        Price = dish.Price,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = username
                    };

                    total += dish.Price * itemDto.Quantity;
                    _context.OrderItems.Add(orderItem);
                }

                if (total < 0.01m)
                {
                    _logger.LogWarning("Попытка создания заказа {OrderId} с нулевой суммой", order.Id);
                    return BadRequest("Сумма заказа должна быть больше 0");
                }

                order.Total = total;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} создан пользователем {UserId}. Сумма: {Total}, Блюд: {ItemsCount}",
                    order.Id, createDto.UserId, total, createDto.Items.Count);

                var readDto = new OrderReadDto
                {
                    Id = order.Id,
                    Username = user.Username,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    Total = order.Total
                };

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, readDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа для пользователя {UserId}", createDto.UserId);
                return StatusCode(500, "Произошла ошибка при создании заказа.");
            }
        }

        // PUT: api/Orders/5 - официанты и админы могут обновлять статус
        [HttpPut("{id}")]
        [Authorize(Policy = "Waiter")]
        public async Task<IActionResult> UpdateOrder(Guid id, OrderUpdateDto updateDto)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                if (order == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего заказа: {OrderId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                var oldStatus = order.Status;
                order.Status = updateDto.Status;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} обновлен. Статус изменен с {OldStatus} на {NewStatus} пользователем {Username}",
                    id, oldStatus, updateDto.Status, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении заказа {OrderId}", id);
                return StatusCode(500, "Произошла ошибка при обновлении заказа.");
            }
        }

        // DELETE: api/Orders/5 - только админ
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                if (order == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего заказа: {OrderId}", id);
                    return NotFound();
                }

                var username = User?.Identity?.Name ?? "System";

                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow;
                order.DeletedBy = username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} удален администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа {OrderId}", id);
                return StatusCode(500, "Произошла ошибка при удалении заказа.");
            }
        }
    }
}