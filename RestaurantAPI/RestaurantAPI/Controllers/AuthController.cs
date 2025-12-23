using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using RestaurantAPI.Constants;
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

        /// <summary>
        /// Регистрация нового пользователя с ролью User
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto.Password != dto.ConfirmPassword)
                {
                    return BadRequest("Пароли не совпадают");
                }

                var (isValid, errorMessage) = _passwordService.ValidatePasswordRequirements(dto.Password);
                if (!isValid)
                {
                    return BadRequest(errorMessage);
                }

                if (await _context.Users.AnyAsync(u => u.Username == dto.Username && !u.IsDeleted))
                {
                    _logger.LogWarning("Попытка регистрации с существующим именем пользователя: {Username}", dto.Username);
                    return BadRequest("Пользователь уже существует.");
                }

                var hashedPassword = _passwordService.HashPassword(dto.Password);

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == Roles.User && !r.IsDeleted);
                if (role == null)
                {
                    _logger.LogError("Роль 'User' не найдена при регистрации");
                    return BadRequest("Роль 'User' не найдена. Обратитесь к администратору.");
                }

                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    RoleId = role.Id
                };

                await _context.Users.AddAsync(user);
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

        /// <summary>
        /// Аутентификация пользователя и выдача JWT токена
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && !u.IsDeleted);
                
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

            var roleName = user.Role?.Name ?? Roles.User;

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
