using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            RestaurantDbContext context, 
            IConfiguration config,
            IPasswordService passwordService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _passwordService = passwordService;
            _logger = logger;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserLoginDto dto)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                {
                    _logger.LogWarning("Попытка регистрации с существующим именем пользователя: {Username}", dto.Username);
                    return BadRequest("Пользователь уже существует.");
                }

                var hashedPassword = _passwordService.HashPassword(dto.Password);

                var roleId = new Guid("be81b9ce-fd34-47d4-b9e4-82c0e811a9ab"); 
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
                if (role == null)
                {
                    _logger.LogError("Роль с Id {RoleId} не найдена при регистрации", roleId);
                    return BadRequest("Роль не найдена");
                }

                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    RoleId = role.Id
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {Username} успешно зарегистрирован с ролью {RoleName}", 
                    user.Username, role.Name);

                return Ok("Пользователь успешно зарегистрирован.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя {Username}", dto.Username);
                return StatusCode(500, "Произошла ошибка при регистрации.");
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == dto.Username);
                
                if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Неудачная попытка входа для пользователя: {Username}", dto.Username);
                    return Unauthorized("Неверный логин или пароль.");
                }

                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(2);

                var jwtToken = new JwtToken
                {
                    UserId = user.Id,
                    Token = token,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    Revoked = false
                };

                _context.JwtTokens.Add(jwtToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {Username} успешно вошел в систему", user.Username);

                return new AuthResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя {Username}", dto.Username);
                return StatusCode(500, "Произошла ошибка при входе.");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"] ?? "RestaurantAPI";

            var roleName = user.Role?.Name ?? "User";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, roleName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
