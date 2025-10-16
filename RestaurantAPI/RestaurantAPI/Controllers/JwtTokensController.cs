using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;
using RestaurantAPI.DTOs;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JwtTokensController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public JwtTokensController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/JwtTokens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JwtTokenReadDto>>> GetTokens()
        {
            var tokens = await _context.JwtTokens
                .Include(t => t.User)
                .Select(t => new JwtTokenReadDto
                {
                    Id = t.Id,
                    Token = t.Token,
                    ExpiresAt = t.ExpiresAt,
                    Revoked = t.Revoked
                })
                .ToListAsync();

            return Ok(tokens);
        }

        // GET: api/JwtTokens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JwtTokenReadDto>> GetToken(Guid id)
        {
            var token = await _context.JwtTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (token == null)
                return NotFound();

            var dto = new JwtTokenReadDto
            {
                Id = token.Id,
                Token = token.Token,
                ExpiresAt = token.ExpiresAt,
                Revoked = token.Revoked
            };

            return Ok(dto);
        }

        // PUT: api/JwtTokens/revoke/5
        [HttpPut("revoke/{id}")]
        public async Task<IActionResult> RevokeToken(Guid id)
        {
            var token = await _context.JwtTokens.FirstOrDefaultAsync(t => t.Id == id);
            if (token == null)
                return NotFound();

            // если уже отозван — ничего не делаем
            if (token.Revoked)
                return BadRequest("Token already revoked");

            var username = User?.Identity?.Name ?? "System";

            token.Revoked = true;
            token.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/JwtTokens/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<JwtTokenReadDto>>> GetActiveTokens()
        {
            var now = DateTime.UtcNow;

            var tokens = await _context.JwtTokens
                .Where(t => !t.Revoked && t.ExpiresAt > now)
                .Select(t => new JwtTokenReadDto
                {
                    Id = t.Id,
                    Token = t.Token,
                    ExpiresAt = t.ExpiresAt,
                    Revoked = t.Revoked
                })
                .ToListAsync();

            return Ok(tokens);
        }
    }
}
