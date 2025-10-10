using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public UsersController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => !u.IsDeleted)
                .Select(u => new UserReadDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    RoleName = u.Role.Name
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound();

            var dto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                RoleName = user.Role.Name
            };

            return Ok(dto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto createDto)
        {
            var username = User?.Identity?.Name ?? "System";

            // Проверяем, что роль существует
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == createDto.RoleId);
            if (role == null)
                return BadRequest("Role not found.");

            // Хэшируем пароль
            var passwordHash = HashPassword(createDto.Password);

            var user = new User
            {
                Username = createDto.Username,
                PasswordHash = passwordHash,
                RoleId = createDto.RoleId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var readDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                RoleName = role.Name
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, readDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto updateDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            if (!string.IsNullOrWhiteSpace(updateDto.Password))
            {
                user.PasswordHash = HashPassword(updateDto.Password);
            }

            // Проверяем, что роль существует
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == updateDto.RoleId);
            if (role == null)
                return BadRequest("Role not found.");

            user.RoleId = updateDto.RoleId;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
                return NotFound();

            var username = User?.Identity?.Name ?? "System";

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = username;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Вспомогательный метод для хэширования пароля
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
