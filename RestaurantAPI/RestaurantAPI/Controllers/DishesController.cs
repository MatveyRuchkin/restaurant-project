using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public DishesController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Dishes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DishReadDto>>> GetDishes()
        {
            var dishes = await _context.Dishes
                .Include(d => d.Category)
                .Where(d => !d.IsDeleted)
                .Select(d => new DishReadDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    Price = d.Price,
                    CategoryName = d.Category.Name
                })
                .ToListAsync();

            return Ok(dishes);
        }

        // GET: api/Dishes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DishReadDto>> GetDish(Guid id)
        {
            var dish = await _context.Dishes
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (dish == null)
                return NotFound();

            var dto = new DishReadDto
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price,
                CategoryName = dish.Category.Name
            };

            return Ok(dto);
        }

        // POST: api/Dishes
        [HttpPost]
        public async Task<ActionResult<DishReadDto>> CreateDish(DishCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System"; 

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && !c.IsDeleted);
            if (category == null)
                return BadRequest("Category not found.");

            var dish = new Dish
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                CategoryId = createDto.CategoryId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            var readDto = new DishReadDto
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price,
                CategoryName = category.Name
            };

            return CreatedAtAction(nameof(GetDish), new { id = dish.Id }, readDto);
        }

        // PUT: api/Dishes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDish(Guid id, DishUpdateDto updateDto)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (dish == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && !c.IsDeleted);
            if (category == null)
                return BadRequest("Category not found.");

            dish.Name = updateDto.Name;
            dish.Description = updateDto.Description;
            dish.Price = updateDto.Price;
            dish.CategoryId = updateDto.CategoryId;
            dish.UpdatedAt = DateTime.UtcNow;
            dish.UpdatedBy = username;
           
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Dishes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDish(Guid id)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (dish == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            dish.IsDeleted = true;
            dish.DeletedAt = DateTime.UtcNow;
            dish.DeletedBy = username;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
