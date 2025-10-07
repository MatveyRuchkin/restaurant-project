namespace RestaurantAPI.DTOs
{
    public class RoleReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class RoleCreateDto
    {
        public string Name { get; set; } = null!;
    }

    public class RoleUpdateDto
    {
        public string Name { get; set; } = null!;
    }

}
