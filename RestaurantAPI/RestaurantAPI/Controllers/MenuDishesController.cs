using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuDishesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public MenuDishesController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/MenuDishes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuDishReadDto>>> GetMenuDishes()
        {
            var menuDishes = await _context.MenuDishes
                .Include(md => md.Menu)
                .Include(md => md.Dish)
                .Where(md => !md.IsDeleted)
                .Select(md => new MenuDishReadDto
                {
                    Id = md.Id,
                    MenuId = md.MenuId,
                    MenuName = md.Menu.Name,
                    DishId = md.DishId,
                    DishName = md.Dish.Name
                })
                .ToListAsync();

            return Ok(menuDishes);
        }

        // GET: api/MenuDishes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuDishReadDto>> GetMenuDish(int id)
        {
            var menuDish = await _context.MenuDishes
                .Include(md => md.Menu)
                .Include(md => md.Dish)
                .FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);

            if (menuDish == null)
                return NotFound();

            var dto = new MenuDishReadDto
            {
                Id = menuDish.Id,
                MenuId = menuDish.MenuId,
                MenuName = menuDish.Menu.Name,
                DishId = menuDish.DishId,
                DishName = menuDish.Dish.Name
            };

            return Ok(dto);
        }

        // POST: api/MenuDishes
        [HttpPost]
        public async Task<ActionResult<MenuDishReadDto>> CreateMenuDish(MenuDishCreateDto createDto)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == createDto.MenuId && !m.IsDeleted);
            if (menu == null)
                return BadRequest("Menu not found");

            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == createDto.DishId && !d.IsDeleted);
            if (dish == null)
                return BadRequest("Dish not found");

            var username = User?.Identity?.Name ?? "System";

            var menuDish = new MenuDish
            {
                MenuId = createDto.MenuId,
                DishId = createDto.DishId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.MenuDishes.Add(menuDish);
            await _context.SaveChangesAsync();

            var readDto = new MenuDishReadDto
            {
                Id = menuDish.Id,
                MenuId = menuDish.MenuId,
                MenuName = menu.Name,
                DishId = menuDish.DishId,
                DishName = dish.Name
            };

            return CreatedAtAction(nameof(GetMenuDish), new { id = menuDish.Id }, readDto);
        }

        // PUT: api/MenuDishes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenuDish(int id, MenuDishUpdateDto updateDto)
        {
            var menuDish = await _context.MenuDishes.FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);
            if (menuDish == null)
                return NotFound();

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == updateDto.MenuId && !m.IsDeleted);
            if (menu == null)
                return BadRequest("Menu not found");

            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == updateDto.DishId && !d.IsDeleted);
            if (dish == null)
                return BadRequest("Dish not found");

            var username = User?.Identity?.Name ?? "System";

            menuDish.MenuId = updateDto.MenuId;
            menuDish.DishId = updateDto.DishId;
            menuDish.UpdatedAt = DateTime.UtcNow;
            menuDish.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/MenuDishes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenuDish(int id)
        {
            var menuDish = await _context.MenuDishes.FirstOrDefaultAsync(md => md.Id == id && !md.IsDeleted);
            if (menuDish == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            menuDish.IsDeleted = true;
            menuDish.DeletedAt = DateTime.UtcNow;
            menuDish.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}