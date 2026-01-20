using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Domain.Entities;
using RestaurantAPI.Infrastructure.Persistence;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(RestaurantDbContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Statistics/overview - общая статистика (только для админов)
        [HttpGet("overview")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> GetOverview(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var query = _context.Orders.Where(o => !o.IsDeleted);

                if (startDate.HasValue)
                {
                    // Используем UTC время
                    var utcStartDate = startDate.Value.Kind == DateTimeKind.Local 
                        ? startDate.Value.ToUniversalTime() 
                        : (startDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                            : startDate.Value);
                    query = query.Where(o => o.CreatedAt >= utcStartDate);
                }

                if (endDate.HasValue)
                {
                    // Используем UTC время
                    var utcEndDate = endDate.Value.Kind == DateTimeKind.Local 
                        ? endDate.Value.ToUniversalTime() 
                        : (endDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                            : endDate.Value);
                    query = query.Where(o => o.CreatedAt <= utcEndDate);
                }

                var totalRevenue = await query.SumAsync(o => o.Total);
                var totalOrders = await query.CountAsync();
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
                var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
                var totalDishes = await _context.Dishes.CountAsync(d => !d.IsDeleted);

                var statusStats = await query
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                _logger.LogInformation("Получена общая статистика. Выручка: {Revenue}, Заказов: {Orders}", 
                    totalRevenue, totalOrders);

                return Ok(new
                {
                    totalRevenue,
                    totalOrders,
                    averageOrderValue,
                    totalUsers,
                    totalDishes,
                    statusStatistics = statusStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении общей статистики");
                return StatusCode(500, "Произошла ошибка при получении статистики.");
            }
        }

        // GET: api/Statistics/revenue-by-date - выручка по датам (только для админов)
        [HttpGet("revenue-by-date")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> GetRevenueByDate(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string period = "day") // day, week, month, year
        {
            try
            {
                var query = _context.Orders.Where(o => !o.IsDeleted);

                if (startDate.HasValue)
                {
                    // Используем UTC время
                    var utcStartDate = startDate.Value.Kind == DateTimeKind.Local 
                        ? startDate.Value.ToUniversalTime() 
                        : (startDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                            : startDate.Value);
                    query = query.Where(o => o.CreatedAt >= utcStartDate);
                }
                else
                {
                    // По умолчанию последние 30 дней (UTC)
                    startDate = DateTime.UtcNow.AddDays(-30);
                    query = query.Where(o => o.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // Используем UTC время
                    var utcEndDate = endDate.Value.Kind == DateTimeKind.Local 
                        ? endDate.Value.ToUniversalTime() 
                        : (endDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                            : endDate.Value);
                    query = query.Where(o => o.CreatedAt <= utcEndDate);
                }
                
                // Нормализуем период
                var normalizedPeriod = (period ?? "day").ToLower().Trim();
                if (normalizedPeriod != "day" && normalizedPeriod != "week" && 
                    normalizedPeriod != "month" && normalizedPeriod != "year")
                {
                    normalizedPeriod = "day";
                    _logger.LogWarning("Неизвестный период '{Period}', используется 'day' по умолчанию", period);
                }
                
                _logger.LogInformation("Получен запрос статистики выручки по датам. Period: {Period} (нормализован: {NormalizedPeriod}), StartDate: {StartDate}, EndDate: {EndDate}", 
                    period, normalizedPeriod, startDate, endDate);
                
                var revenueData = normalizedPeriod switch
                {
                    "week" => (await query
                        .Select(o => new
                        {
                            OrderDate = o.CreatedAt,
                            Total = o.Total
                        })
                        .ToListAsync())
                        .GroupBy(o =>
                        {
                            var date = o.OrderDate.Date;
                            var dayOfWeek = (int)date.DayOfWeek;
                            var mondayOffset = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
                            return date.AddDays(mondayOffset);
                        })
                        .Select(g => new
                        {
                            date = g.Key.ToString("yyyy-MM-dd"),
                            revenue = g.Sum(o => o.Total),
                            ordersCount = g.Count()
                        })
                        .OrderBy(x => x.date)
                        .ToList(),
                    "month" => (await query
                        .Select(o => new
                        {
                            Date = o.CreatedAt,
                            Total = o.Total
                        })
                        .ToListAsync())
                        .GroupBy(o => new { o.Date.Year, o.Date.Month })
                        .Select(g => new
                        {
                            date = g.Key.Year + "-" + g.Key.Month.ToString("D2"),
                            revenue = g.Sum(o => o.Total),
                            ordersCount = g.Count()
                        })
                        .OrderBy(x => x.date)
                        .ToList(),
                    "year" => (await query
                        .Select(o => new
                        {
                            Date = o.CreatedAt,
                            Total = o.Total
                        })
                        .ToListAsync())
                        .GroupBy(o => o.Date.Year)
                        .Select(g => new
                        {
                            date = g.Key.ToString("D4"),
                            revenue = g.Sum(o => o.Total),
                            ordersCount = g.Count()
                        })
                        .OrderBy(x => x.date)
                        .ToList(),
                    _ => (await query
                        .Select(o => new
                        {
                            Date = o.CreatedAt,
                            Total = o.Total
                        })
                        .ToListAsync())
                        .GroupBy(o => new 
                        { 
                            Year = o.Date.Year, 
                            Month = o.Date.Month, 
                            Day = o.Date.Day 
                        })
                        .Select(g => new
                        {
                            date = g.Key.Year + "-" + g.Key.Month.ToString("D2") + "-" + g.Key.Day.ToString("D2"),
                            revenue = g.Sum(o => o.Total),
                            ordersCount = g.Count()
                        })
                        .OrderBy(x => x.date)
                        .ToList()
                };
                
                return Ok(revenueData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики выручки по датам. Period: {Period}, StartDate: {StartDate}, EndDate: {EndDate}", 
                    period, startDate, endDate);
                _logger.LogError(ex, "Полный стек ошибки: {StackTrace}", ex.StackTrace);
                return StatusCode(500, "Произошла ошибка при получении статистики.");
            }
        }

        // GET: api/Statistics/top-dishes - топ блюд (только для админов)
        [HttpGet("top-dishes")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> GetTopDishes(
            int top = 10,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var query = _context.OrderItems
                    .Include(oi => oi.Dish)
                        .ThenInclude(d => d.Category)
                    .Include(oi => oi.Order)
                    .Where(oi => !oi.IsDeleted && !oi.Order.IsDeleted);

                if (startDate.HasValue)
                {
                    // Используем UTC время
                    var utcStartDate = startDate.Value.Kind == DateTimeKind.Local 
                        ? startDate.Value.ToUniversalTime() 
                        : (startDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                            : startDate.Value);
                    query = query.Where(oi => oi.Order.CreatedAt >= utcStartDate);
                }

                if (endDate.HasValue)
                {
                    // Используем UTC время
                    var utcEndDate = endDate.Value.Kind == DateTimeKind.Local 
                        ? endDate.Value.ToUniversalTime() 
                        : (endDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                            : endDate.Value);
                    query = query.Where(oi => oi.Order.CreatedAt <= utcEndDate);
                }

                var topDishes = await query
                    .GroupBy(oi => new { oi.DishId, oi.Dish.Name, CategoryName = oi.Dish.Category.Name, CategoryId = oi.Dish.Category.Id })
                    .Select(g => new
                    {
                        DishId = g.Key.DishId,
                        DishName = g.Key.Name,
                        CategoryName = g.Key.CategoryName,
                        CategoryId = g.Key.CategoryId,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.Price * oi.Quantity),
                        OrderCount = g.Select(oi => oi.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(top)
                    .ToListAsync();

                _logger.LogInformation("Получен топ {Count} блюд", top);

                return Ok(topDishes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении топа блюд");
                return StatusCode(500, "Произошла ошибка при получении статистики.");
            }
        }

        // GET: api/Statistics/revenue-by-category - выручка по категориям (только для админов)
        [HttpGet("revenue-by-category")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> GetRevenueByCategory(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var query = _context.OrderItems
                    .Include(oi => oi.Dish)
                        .ThenInclude(d => d.Category)
                    .Include(oi => oi.Order)
                    .Where(oi => !oi.IsDeleted && !oi.Order.IsDeleted);

                if (startDate.HasValue)
                {
                    // Используем UTC время
                    var utcStartDate = startDate.Value.Kind == DateTimeKind.Local 
                        ? startDate.Value.ToUniversalTime() 
                        : (startDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                            : startDate.Value);
                    query = query.Where(oi => oi.Order.CreatedAt >= utcStartDate);
                }

                if (endDate.HasValue)
                {
                    // Используем UTC время
                    var utcEndDate = endDate.Value.Kind == DateTimeKind.Local 
                        ? endDate.Value.ToUniversalTime() 
                        : (endDate.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                            : endDate.Value);
                    query = query.Where(oi => oi.Order.CreatedAt <= utcEndDate);
                }

                var categoryRevenue = await query
                    .GroupBy(oi => new { oi.Dish.Category.Id, oi.Dish.Category.Name })
                    .Select(g => new
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        TotalRevenue = g.Sum(oi => oi.Price * oi.Quantity),
                        TotalQuantity = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToListAsync();

                return Ok(categoryRevenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики по категориям");
                return StatusCode(500, "Произошла ошибка при получении статистики.");
            }
        }

        // GET: api/Statistics/recent-orders - последние заказы (только для админов)
        [HttpGet("recent-orders")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> GetRecentOrders(int count = 10)
        {
            try
            {
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => !o.IsDeleted)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(count)
                    .Select(o => new
                    {
                        o.Id,
                        OrderDate = o.CreatedAt,
                        o.Status,
                        o.Total,
                        Username = o.User.Username
                    })
                    .ToListAsync();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении последних заказов");
                return StatusCode(500, "Произошла ошибка при получении статистики.");
            }
        }
    }
}

