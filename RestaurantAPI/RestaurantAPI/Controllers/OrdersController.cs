using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public OrdersController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderReadDto>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => !o.IsDeleted)
                .Select(o => new OrderReadDto
                {
                    Id = o.Id,
                    Username = o.User.Username,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    Total = o.Total
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderReadDto>> GetOrder(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null)
                return NotFound();

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

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<OrderReadDto>> CreateOrder(OrderCreateDto createDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == createDto.UserId && !u.IsDeleted);
            if (user == null)
                return BadRequest("User not found");

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
                var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == itemDto.DishId && !d.IsDeleted);
                if (dish == null)
                    continue;

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

            order.Total = total;
            await _context.SaveChangesAsync();

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

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(Guid id, OrderUpdateDto updateDto)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            order.Status = updateDto.Status;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            order.IsDeleted = true;
            order.DeletedAt = DateTime.UtcNow;
            order.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}