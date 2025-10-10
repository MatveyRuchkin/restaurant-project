using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public MenusController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Menus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuReadDto>>> GetMenus()
        {
            var menus = await _context.Menus
                .Where(m => !m.IsDeleted)
                .Select(m => new MenuReadDto
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .ToListAsync();

            return Ok(menus);
        }

        // GET: api/Menus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuReadDto>> GetMenu(int id)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (menu == null)
                return NotFound();

            var dto = new MenuReadDto
            {
                Id = menu.Id,
                Name = menu.Name
            };

            return Ok(dto);
        }

        // POST: api/Menus
        [HttpPost]
        public async Task<ActionResult<MenuReadDto>> CreateMenu(MenuCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            var menu = new Menu
            {
                Name = createDto.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            var readDto = new MenuReadDto
            {
                Id = menu.Id,
                Name = menu.Name
            };

            return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, readDto);
        }

        // PUT: api/Menus/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenu(int id, MenuUpdateDto updateDto)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (menu == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            menu.Name = updateDto.Name;
            menu.UpdatedAt = DateTime.UtcNow;
            menu.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Menus/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (menu == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            menu.IsDeleted = true;
            menu.DeletedAt = DateTime.UtcNow;
            menu.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}