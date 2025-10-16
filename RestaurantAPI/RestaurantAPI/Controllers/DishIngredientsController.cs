using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;
using RestaurantAPI.DTOs;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishIngredientsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public DishIngredientsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/DishIngredients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DishIngredientReadDto>>> GetDishIngredients()
        {
            var dishIngredients = await _context.DishIngredients
                .Include(di => di.Dish)
                .Include(di => di.Ingredient)
                .Where(di => !di.IsDeleted)
                .Select(di => new DishIngredientReadDto
                {
                    Id = di.Id,
                    DishId = di.DishId,
                    DishName = di.Dish.Name,
                    IngredientId = di.IngredientId,
                    IngredientName = di.Ingredient.Name,
                    Quantity = di.Quantity
                })
                .ToListAsync();

            return Ok(dishIngredients);
        }

        // GET: api/DishIngredients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DishIngredientReadDto>> GetDishIngredient(Guid id)
        {
            var di = await _context.DishIngredients
                .Include(x => x.Dish)
                .Include(x => x.Ingredient)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (di == null)
                return NotFound();

            var dto = new DishIngredientReadDto
            {
                Id = di.Id,
                DishId = di.DishId,
                DishName = di.Dish.Name,
                IngredientId = di.IngredientId,
                IngredientName = di.Ingredient.Name,
                Quantity = di.Quantity
            };

            return Ok(dto);
        }

        // POST: api/DishIngredients
        [HttpPost]
        public async Task<ActionResult<DishIngredientReadDto>> CreateDishIngredient(DishIngredientCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            // проверяем наличие блюда и ингредиента
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId && !d.IsDeleted);
            if (dish == null)
                return BadRequest("Dish not found.");

            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == createDto.IngredientId && !i.IsDeleted);
            if (ingredient == null)
                return BadRequest("Ingredient not found.");

            var dishIngredient = new DishIngredient
            {
                DishId = createDto.DishId,
                IngredientId = createDto.IngredientId,
                Quantity = createDto.Quantity,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.DishIngredients.Add(dishIngredient);
            await _context.SaveChangesAsync();

            var readDto = new DishIngredientReadDto
            {
                Id = dishIngredient.Id,
                DishId = dish.Id,
                DishName = dish.Name,
                IngredientId = ingredient.Id,
                IngredientName = ingredient.Name,
                Quantity = dishIngredient.Quantity
            };

            return CreatedAtAction(nameof(GetDishIngredient), new { id = dishIngredient.Id }, readDto);
        }

        // PUT: api/DishIngredients/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDishIngredient(Guid id, DishIngredientUpdateDto updateDto)
        {
            var dishIngredient = await _context.DishIngredients
                .Include(di => di.Ingredient)
                .Include(di => di.Dish)
                .FirstOrDefaultAsync(di => di.Id == id && !di.IsDeleted);

            if (dishIngredient == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            // проверим, что новый ингредиент существует
            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == updateDto.IngredientId && !i.IsDeleted);
            if (ingredient == null)
                return BadRequest("Ingredient not found.");

            dishIngredient.IngredientId = updateDto.IngredientId;
            dishIngredient.Quantity = updateDto.Quantity;
            dishIngredient.UpdatedAt = DateTime.UtcNow;
            dishIngredient.UpdatedBy = username;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/DishIngredients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDishIngredient(Guid id)
        {
            var dishIngredient = await _context.DishIngredients
                .Include(di => di.Ingredient)
                .Include(di => di.Dish)
                .FirstOrDefaultAsync(di => di.Id == id && !di.IsDeleted);

            if (dishIngredient == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            dishIngredient.IsDeleted = true;
            dishIngredient.DeletedAt = DateTime.UtcNow;
            dishIngredient.DeletedBy = username;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
