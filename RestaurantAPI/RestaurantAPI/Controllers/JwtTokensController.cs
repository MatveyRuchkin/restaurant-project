using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Domain.Entities;
using RestaurantAPI.Infrastructure.Persistence;
using RestaurantAPI.DTOs;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")] // Весь контроллер только для админов
    public class JwtTokensController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly ILogger<JwtTokensController> _logger;

        public JwtTokensController(RestaurantDbContext context, ILogger<JwtTokensController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/JwtTokens
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet]
        public async Task<ActionResult> GetTokens(
            Guid? userId = null,
            bool? revoked = null,
            string sortBy = "createdat",
            string order = "desc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.JwtTokens
                    .Include(t => t.User)
                    .AsQueryable();

                // Фильтрация по пользователю
                if (userId.HasValue)
                {
                    query = query.Where(t => t.UserId == userId.Value);
                }

                // Фильтрация по статусу отзыва
                if (revoked.HasValue)
                {
                    query = query.Where(t => t.Revoked == revoked.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "createdat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(t => t.CreatedAt)
                        : query.OrderBy(t => t.CreatedAt),
                    "expiresat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(t => t.ExpiresAt)
                        : query.OrderBy(t => t.ExpiresAt),
                    "revoked" => order.ToLower() == "desc"
                        ? query.OrderByDescending(t => t.Revoked)
                        : query.OrderBy(t => t.Revoked),
                    _ => query.OrderByDescending(t => t.CreatedAt)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var tokens = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new JwtTokenReadDto
                    {
                        Id = t.Id,
                        Token = t.Token,
                        ExpiresAt = t.ExpiresAt,
                        Revoked = t.Revoked
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список токенов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    tokens.Count, totalCount, page);

                return Ok(new
                {
                    data = tokens,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка токенов");
                return StatusCode(500, "Произошла ошибка при получении списка токенов.");
            }
        }

        // GET: api/JwtTokens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JwtTokenReadDto>> GetToken(Guid id)
        {
            try
            {
                var token = await _context.JwtTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (token == null)
                {
                    _logger.LogWarning("Токен с Id {TokenId} не найден", id);
                    return NotFound();
                }

                var dto = new JwtTokenReadDto
                {
                    Id = token.Id,
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt,
                    Revoked = token.Revoked
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении токена {TokenId}", id);
                return StatusCode(500, "Произошла ошибка при получении токена.");
            }
        }

        // PUT: api/JwtTokens/revoke/5
        [HttpPut("revoke/{id}")]
        public async Task<IActionResult> RevokeToken(Guid id)
        {
            try
            {
                var token = await _context.JwtTokens.FirstOrDefaultAsync(t => t.Id == id);
                if (token == null)
                {
                    _logger.LogWarning("Попытка отзыва несуществующего токена: {TokenId}", id);
                    return NotFound();
                }

                if (token.Revoked)
                {
                    _logger.LogWarning("Попытка отзыва уже отозванного токена: {TokenId}", id);
                    return BadRequest("Токен уже отозван.");
                }

                var username = User?.Identity?.Name ?? "System";

                token.Revoked = true;
                token.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Токен {TokenId} отозван администратором {Username}", id, username);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве токена {TokenId}", id);
                return StatusCode(500, "Произошла ошибка при отзыве токена.");
            }
        }

        // GET: api/JwtTokens/active
        // Поддерживает фильтрацию, сортировку и пагинацию
        [HttpGet("active")]
        public async Task<ActionResult> GetActiveTokens(
            Guid? userId = null,
            string sortBy = "expiresat",
            string order = "asc",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var now = DateTime.UtcNow;

                var query = _context.JwtTokens
                    .Where(t => !t.Revoked && t.ExpiresAt > now);

                // Фильтрация по пользователю
                if (userId.HasValue)
                {
                    query = query.Where(t => t.UserId == userId.Value);
                }

                // Сортировка
                query = sortBy.ToLower() switch
                {
                    "expiresat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(t => t.ExpiresAt)
                        : query.OrderBy(t => t.ExpiresAt),
                    "createdat" => order.ToLower() == "desc"
                        ? query.OrderByDescending(t => t.CreatedAt)
                        : query.OrderBy(t => t.CreatedAt),
                    _ => query.OrderBy(t => t.ExpiresAt)
                };

                // Подсчет общего количества
                var totalCount = await query.CountAsync();

                // Пагинация
                var tokens = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new JwtTokenReadDto
                    {
                        Id = t.Id,
                        Token = t.Token,
                        ExpiresAt = t.ExpiresAt,
                        Revoked = t.Revoked
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Получен список активных токенов. Количество: {Count}, Всего: {Total}, Страница: {Page}",
                    tokens.Count, totalCount, page);

                return Ok(new
                {
                    data = tokens,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка активных токенов");
                return StatusCode(500, "Произошла ошибка при получении списка активных токенов.");
            }
        }
    }
}
