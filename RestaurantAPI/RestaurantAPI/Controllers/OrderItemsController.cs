using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public OrderItemsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/OrderItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemReadDto>>> GetOrderItems()
        {
            var items = await _context.OrderItems
                .Include(oi => oi.Dish) // Подтягиваем название блюда
                .Where(oi => !oi.IsDeleted)
                .Select(oi => new OrderItemReadDto
                {
                    Id = oi.Id,
                    DishId = oi.DishId,
                    DishName = oi.Dish.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/OrderItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemReadDto>> GetOrderItem(Guid id)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Dish)
                .FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);

            if (item == null)
                return NotFound();

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

        // POST: api/OrderItems
        [HttpPost]
        public async Task<ActionResult<OrderItemReadDto>> CreateOrderItem(OrderItemCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            // Получаем цену блюда
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId);
            if (dish == null)
                return BadRequest("Dish not found.");

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

        // PUT: api/OrderItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(Guid id, OrderItemUpdateDto updateDto)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Dish)
                .FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);

            if (orderItem == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            orderItem.Quantity = updateDto.Quantity;
            orderItem.Price = orderItem.Dish.Price * updateDto.Quantity;
            orderItem.UpdatedAt = DateTime.UtcNow;
            orderItem.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/OrderItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(Guid id)
        {
            var orderItem = await _context.OrderItems.FirstOrDefaultAsync(oi => oi.Id == id && !oi.IsDeleted);
            if (orderItem == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            orderItem.IsDeleted = true;
            orderItem.DeletedAt = DateTime.UtcNow;
            orderItem.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
