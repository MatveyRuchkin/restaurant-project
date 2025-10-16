using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public CategoriesController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryReadDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryReadDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Notes = c.Notes
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryReadDto>> GetCategory(Guid id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null)
                return NotFound();

            var dto = new CategoryReadDto
            {
                Id = category.Id,
                Name = category.Name,
                Notes = category.Notes
            };

            return Ok(dto);
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<CategoryReadDto>> CreateCategory(CategoryCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System"; 

            var category = new Category
            {
                Name = createDto.Name,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var readDto = new CategoryReadDto
            {
                Id = category.Id,
                Name = category.Name,
                Notes = category.Notes
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, readDto);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, CategoryUpdateDto updateDto)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            category.Name = updateDto.Name;
            category.Notes = updateDto.Notes;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
