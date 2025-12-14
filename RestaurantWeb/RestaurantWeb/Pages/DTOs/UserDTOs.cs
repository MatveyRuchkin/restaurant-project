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
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid RoleId { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Password { get; set; }
        public Guid RoleId { get; set; }
    }

    public class UserLoginDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

}
