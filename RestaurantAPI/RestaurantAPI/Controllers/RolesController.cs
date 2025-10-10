using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public RolesController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleReadDto>>> GetRoles()
        {
            var roles = await _context.Roles
                .Where(r => !r.IsDeleted)
                .Select(r => new RoleReadDto
                {
                    Id = r.Id,
                    Name = r.Name
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(int id)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (role == null)
                return NotFound();

            var dto = new RoleReadDto
            {
                Id = role.Id,
                Name = role.Name
            };

            return Ok(dto);
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole(RoleCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            var role = new Role
            {
                Name = createDto.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var readDto = new RoleReadDto
            {
                Id = role.Id,
                Name = role.Name
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, readDto);
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, RoleUpdateDto updateDto)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (role == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            role.Name = updateDto.Name;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (role == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            role.IsDeleted = true;
            role.DeletedAt = DateTime.UtcNow;
            role.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}