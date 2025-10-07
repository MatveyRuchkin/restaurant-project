namespace RestaurantAPI.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }

    public class UserCreateDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int RoleId { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Password { get; set; }
        public int RoleId { get; set; }
    }

}
