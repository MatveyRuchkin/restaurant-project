using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantAPI.DTOs;
using RestaurantAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RestaurantDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(RestaurantDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserLoginDto dto)
        {
            // Проверка на существующего пользователя
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Пользователь уже существует.");

            // Хэширование пароля
            var hashedPassword = HashPassword(dto.Password);

            // Назначаем роль с Id = 2
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == 2);
            if (role == null)
                return BadRequest("Роль с Id = 2 не найдена");

            // Создаём пользователя с RoleId = 2
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                RoleId = role.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Пользователь успешно зарегистрирован.");
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized("Неверный логин или пароль.");

            // Генерация токена
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(2);

            // Сохраняем токен в БД
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

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt
            };
        }

        // ================ PRIVATE HELPERS ==================

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "K1a5i0s8e0r5k2a0y0a5oMlOeTsYyAaGODBLESSAMERICA";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "RestaurantAPI";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("userId", user.Id.ToString())
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

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hash = HashPassword(password);
            return hash == hashedPassword;
        }
    }
}
