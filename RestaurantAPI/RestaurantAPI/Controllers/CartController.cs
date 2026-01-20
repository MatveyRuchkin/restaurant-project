using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Helpers;
using RestaurantAPI.Domain.Entities;
using RestaurantAPI.Infrastructure.Persistence;
using RestaurantAPI.Constants;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : BaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CartController> _logger;

        public CartController(
            RestaurantDbContext context,
            IMapper mapper,
            ILogger<CartController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Получение корзины текущего пользователя. Создается автоматически, если не существует
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CartReadDto>> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Dish)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Carts.AddAsync(cart);
                    await _context.SaveChangesAsync();
                    
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Dish)
                        .FirstOrDefaultAsync(c => c.UserId == userId.Value);
                }

                var cartDto = _mapper.Map<CartReadDto>(cart);
                
                // Фильтрация удаленных блюд и пересчет итогов
                cartDto.Items = cart.CartItems
                    .Where(ci => !ci.Dish.IsDeleted)
                    .Select(ci => _mapper.Map<CartItemReadDto>(ci))
                    .ToList();
                
                cartDto.Total = cartDto.Items.Sum(i => i.Subtotal);
                cartDto.TotalItems = cartDto.Items.Sum(i => i.Quantity);

                return Ok(cartDto);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении корзины");
                throw;
            }
        }

        /// <summary>
        /// Добавление товара в корзину. Если товар уже есть с теми же примечаниями, увеличивается количество
        /// </summary>
        [HttpPost("items")]
        public async Task<ActionResult<CartReadDto>> AddItem([FromBody] CartItemAddDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Carts.AddAsync(cart);
                    await _context.SaveChangesAsync();
                    
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.UserId == userId.Value);
                }

                var dish = await _context.Dishes
                    .FirstOrDefaultAsync(d => d.Id == dto.DishId && !d.IsDeleted);

                if (dish == null || dish.IsDeleted)
                {
                    _logger.LogWarning("Попытка добавить несуществующее блюдо {DishId} в корзину пользователя {UserId}",
                        dto.DishId, userId);
                    throw new BadRequestException("Блюдо не найдено или недоступно");
                }

                var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
                var existingItem = cart.CartItems
                    .FirstOrDefault(ci => ci.DishId == dto.DishId && ci.Notes == notes);

                if (existingItem != null)
                {
                    existingItem.Quantity += dto.Quantity;
                    if (existingItem.Quantity > 100)
                    {
                        existingItem.Quantity = 100;
                    }
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CartId = cart.Id,
                        DishId = dto.DishId,
                        Quantity = dto.Quantity,
                        Notes = notes,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Товар {DishId} добавлен в корзину пользователя {UserId}", dto.DishId, userId);

                return await GetCart();
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в корзину");
                throw;
            }
        }

        /// <summary>
        /// Обновление количества и примечаний элемента корзины
        /// </summary>
        [HttpPut("items/{id}")]
        public async Task<ActionResult<CartReadDto>> UpdateItem(Guid id, [FromBody] CartItemUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    throw new NotFoundException("Корзина не найдена");
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == id);
                if (cartItem == null)
                {
                    _logger.LogWarning("Попытка обновить несуществующий элемент корзины {ItemId} пользователя {UserId}",
                        id, userId);
                    throw new NotFoundException("Элемент корзины не найден");
                }

                cartItem.Quantity = dto.Quantity;
                cartItem.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
                cartItem.UpdatedAt = DateTime.UtcNow;
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Элемент корзины {ItemId} обновлен пользователем {UserId}", id, userId);

                return await GetCart();
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении элемента корзины");
                throw;
            }
        }

        /// <summary>
        /// Удаление элемента из корзины
        /// </summary>
        [HttpDelete("items/{id}")]
        public async Task<ActionResult<CartReadDto>> RemoveItem(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    throw new NotFoundException("Корзина не найдена");
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == id);
                if (cartItem == null)
                {
                    _logger.LogWarning("Попытка удалить несуществующий элемент корзины {ItemId} пользователя {UserId}",
                        id, userId);
                    throw new NotFoundException("Элемент корзины не найден");
                }

                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Элемент корзины {ItemId} удален пользователем {UserId}", id, userId);

                return await GetCart();
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении элемента корзины");
                throw;
            }
        }

        /// <summary>
        /// Очистка корзины (удаление всех элементов)
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedException("Не удалось определить пользователя");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Корзина пользователя {UserId} очищена", userId);
                }

                return NoContent();
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке корзины");
                throw;
            }
        }
    }
}
