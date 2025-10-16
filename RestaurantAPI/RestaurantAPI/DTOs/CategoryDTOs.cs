namespace RestaurantAPI.DTOs
{
    public class CategoryReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Notes { get; set; }
    }

    public class CategoryCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Notes { get; set; }
    }

    public class CategoryUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Notes { get; set; }
    }
}
