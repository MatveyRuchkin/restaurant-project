using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class UserReadDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }

    public class UserCreateDto
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 100 символов")]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Имя пользователя может содержать только буквы, цифры и подчеркивания")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Роль обязательна")]
        public Guid RoleId { get; set; }
    }

    public class UserUpdateDto
    {
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        public Guid RoleId { get; set; }
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = null!;
    }

    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 100 символов")]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Имя пользователя может содержать только буквы, цифры и подчеркивания")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 100 символов")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
