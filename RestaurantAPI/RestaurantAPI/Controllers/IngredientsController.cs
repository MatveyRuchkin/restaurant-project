using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;
using RestaurantApi.DTOs;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public IngredientsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Ingredients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientReadDto>>> GetIngredients()
        {
            var ingredients = await _context.Ingredients
                .Where(i => !i.IsDeleted)
                .Select(i => new IngredientReadDto
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync();

            return Ok(ingredients);
        }

        // GET: api/Ingredients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientReadDto>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (ingredient == null)
                return NotFound();

            var dto = new IngredientReadDto
            {
                Id = ingredient.Id,
                Name = ingredient.Name
            };

            return Ok(dto);
        }

        // POST: api/Ingredients
        [HttpPost]
        public async Task<ActionResult<IngredientReadDto>> CreateIngredient(IngredientCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            var ingredient = new Ingredient
            {
                Name = createDto.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();

            var readDto = new IngredientReadDto
            {
                Id = ingredient.Id,
                Name = ingredient.Name
            };

            return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, readDto);
        }

        // PUT: api/Ingredients/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIngredient(int id, IngredientUpdateDto updateDto)
        {
            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (ingredient == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            ingredient.Name = updateDto.Name;
            ingredient.UpdatedAt = DateTime.UtcNow;
            ingredient.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Ingredients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (ingredient == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            ingredient.IsDeleted = true;
            ingredient.DeletedAt = DateTime.UtcNow;
            ingredient.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
