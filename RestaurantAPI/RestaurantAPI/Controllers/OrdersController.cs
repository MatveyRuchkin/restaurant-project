using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Domain.Entities;
using RestaurantAPI.Constants;
using RestaurantAPI.Helpers;
using RestaurantAPI.Application.Interfaces;
using RestaurantAPI.Infrastructure.Persistence;
using RestaurantAPI.Exceptions;
using System.Security.Claims;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IBusinessValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            RestaurantDbContext context,
            IBusinessValidationService validationService,
            IMapper mapper,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Получение заказов текущего пользователя с пагинацией
        /// </summary>
        [HttpGet("MyOrders")]
        [Authorize]
        public async Task<ActionResult<PagedResult<OrderReadDto>>> GetMyOrders(
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var query = _context.Orders
                    .Where(o => o.UserId == userId.Value && !o.IsDeleted)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt);

                var totalCount = await query.CountAsync();

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = _mapper.Map<IEnumerable<OrderReadDto>>(orders);

                _logger.LogInformation("Получены заказы пользователя {UserId}. Количество: {Count}, Всего: {Total}, Страница: {Page}", 
                    userId.Value, orders.Count, totalCount, page);

                return Ok(new PagedResult<OrderReadDto>
                {
                    Data = orderDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказов пользователя");
                throw;
            }
        }

        /// <summary>
        /// Получение всех заказов (только для официантов и админов)
        /// Поддерживает фильтрацию по userId, status, дате, сумме, сортировку и пагинацию
        /// </summary>
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
                    .Where(o => !o.IsDeleted)
                    .Include(o => o.User)
                    .AsQueryable();

                if (userId.HasValue)
                {
                    query = query.Where(o => o.UserId == userId.Value);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                // Фильтрация по дате (используем CreatedAt вместо OrderDate)
                if (startDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= endDate.Value);
                }

                if (minTotal.HasValue)
                {
                    query = query.Where(o => o.Total >= minTotal.Value);
                }

                if (maxTotal.HasValue)
                {
                    query = query.Where(o => o.Total <= maxTotal.Value);
                }

                query = sortBy.ToLower() switch
                {
                    "total" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.Total)
                        : query.OrderBy(o => o.Total),
                    "orderdate" or "createdat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.CreatedAt)
                        : query.OrderBy(o => o.CreatedAt),
                    "status" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.Status)
                        : query.OrderBy(o => o.Status),
                    "username" => order.ToLower() == "desc"
                        ? query.OrderByDescending(o => o.User.Username)
                        : query.OrderBy(o => o.User.Username),
                    _ => query.OrderByDescending(o => o.CreatedAt)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = _mapper.Map<IEnumerable<OrderReadDto>>(orders);

                _logger.LogInformation(
                    "Получен список заказов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    orders.Count, totalCount, page);

                return Ok(new PagedResult<OrderReadDto>
                {
                    Data = orderDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка заказов");
                throw;
            }
        }

        /// <summary>
        /// Получение заказа по ID. Пользователи видят только свои заказы, официанты и админы - все
        /// </summary>
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
                    throw new NotFoundException("Заказ не найден");
                }

                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (!IsAdmin() && !IsWaiter() &&
                    userId.HasValue && userId.Value != order.UserId)
                {
                    _logger.LogWarning("Попытка доступа к чужому заказу {OrderId} пользователем {UserId}",
                        id, userId);
                    throw new ForbiddenException("Доступ запрещен");
                }

                var dto = _mapper.Map<OrderReadDto>(order);
                return Ok(dto);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ForbiddenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказа {OrderId}", id);
                throw;
            }
        }

        /// <summary>
        /// Создание нового заказа. Любой авторизованный пользователь может создать заказ только для себя
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderReadDto>> CreateOrder(OrderCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (!IsAdmin() && !IsWaiter() &&
                    userId.HasValue && userId.Value != createDto.UserId)
                {
                    _logger.LogWarning("Попытка создания заказа для другого пользователя. UserId: {UserId}, RequestedUserId: {RequestedUserId}",
                        userId, createDto.UserId);
                    throw new ForbiddenException("Недостаточно прав для создания заказа для другого пользователя");
                }

                await _validationService.ValidateOrderCreationAsync(createDto);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == createDto.UserId && !u.IsDeleted);
                var currentUsername = GetCurrentUsername();
                
                // Для пользователя "generator" используем его имя в CreatedBy, иначе - имя текущего пользователя
                var createdBy = user != null && user.Username.Equals("generator", StringComparison.OrdinalIgnoreCase)
                    ? "generator"
                    : currentUsername;
                
                decimal total = 0;
                
                _logger.LogInformation("Создание заказа. UserId: {UserId}, Notes: {Notes}, ItemsCount: {ItemsCount}", 
                    createDto.UserId, createDto.Notes ?? "null", createDto.Items?.Count ?? 0);
                
                // Используем UTC время
                DateTime createdAtValue;
                if (createDto.OrderDate.HasValue)
                {
                    var incomingDate = createDto.OrderDate.Value;
                    // Используем UTC время
                    if (incomingDate.Kind == DateTimeKind.Local)
                    {
                        createdAtValue = incomingDate.ToUniversalTime();
                    }
                    else if (incomingDate.Kind == DateTimeKind.Unspecified)
                    {
                        // Предполагаем, что это UTC время
                        createdAtValue = DateTime.SpecifyKind(incomingDate, DateTimeKind.Utc);
                    }
                    else
                    {
                        createdAtValue = incomingDate; // Уже UTC время
                    }
                }
                else
                {
                    createdAtValue = DateTime.UtcNow; // UTC время
                }
                
                _logger.LogInformation("Создание заказа. Входящая дата: {IncomingDate}, Kind: {Kind}, Результирующая дата: {CreatedAt}, Year: {Year}", 
                    createDto.OrderDate, 
                    createDto.OrderDate?.Kind, 
                    createdAtValue, 
                    createdAtValue.Year);

                var order = new Order
                {
                    UserId = createDto.UserId,
                    Status = OrderStatuses.Pending,
                    Notes = !string.IsNullOrWhiteSpace(createDto.Notes) ? createDto.Notes.Trim() : null,
                    CreatedAt = createdAtValue,
                    CreatedBy = createdBy
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var itemDto in createDto.Items)
                {
                    var dish = await _context.Dishes
                        .FirstOrDefaultAsync(d => d.Id == itemDto.DishId && !d.IsDeleted);
                    
                    _logger.LogInformation("Добавление элемента заказа. DishId: {DishId}, Quantity: {Quantity}, Notes: {Notes}", 
                        itemDto.DishId, itemDto.Quantity, itemDto.Notes ?? "null");
                    
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        DishId = itemDto.DishId,
                        Quantity = itemDto.Quantity,
                        Price = dish!.Price,
                        Notes = !string.IsNullOrWhiteSpace(itemDto.Notes) ? itemDto.Notes.Trim() : null,
                        CreatedAt = DateTime.UtcNow, // UTC время
                        CreatedBy = createdBy
                    };

                    total += dish.Price * itemDto.Quantity;
                    _context.OrderItems.Add(orderItem);
                }

                order.Total = total;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Заказ {OrderId} создан пользователем {UserId}. Сумма: {Total}, Блюд: {ItemsCount}",
                    order.Id, createDto.UserId, total, createDto.Items.Count);

                var readDto = _mapper.Map<OrderReadDto>(order);
                readDto.Username = user!.Username;

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, readDto);
            }
            catch (BadRequestException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (ForbiddenException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при создании заказа для пользователя {UserId}", createDto.UserId);
                throw;
            }
        }

        /// <summary>
        /// Обновление статуса заказа (только для официантов и админов)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Waiter")]
        public async Task<IActionResult> UpdateOrder(Guid id, OrderUpdateDto updateDto)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                if (order == null)
                {
                    _logger.LogWarning("Попытка обновления несуществующего заказа: {OrderId}", id);
                    throw new NotFoundException("Заказ не найден");
                }

                await _validationService.ValidateOrderStatusChangeAsync(order, updateDto.Status);

                var username = GetCurrentUsername();

                var oldStatus = order.Status;
                order.Status = updateDto.Status;
                order.UpdatedAt = DateTime.UtcNow; // UTC время
                order.UpdatedBy = username;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} обновлен. Статус изменен с {OldStatus} на {NewStatus} пользователем {Username}",
                    id, oldStatus, updateDto.Status, username);

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
                _logger.LogError(ex, "Ошибка при обновлении заказа {OrderId}", id);
                throw;
            }
        }

        /// <summary>
        /// Мягкое удаление заказа (только для админов)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                if (order == null)
                {
                    _logger.LogWarning("Попытка удаления несуществующего заказа: {OrderId}", id);
                    throw new NotFoundException("Заказ не найден");
                }

                var username = GetCurrentUsername();

                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow; // UTC время
                order.DeletedBy = username;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} удален администратором {Username}", id, username);

                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа {OrderId}", id);
                throw;
            }
        }
    }
}