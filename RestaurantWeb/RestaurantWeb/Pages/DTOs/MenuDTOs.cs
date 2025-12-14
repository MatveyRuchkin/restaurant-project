namespace RestaurantAPI.DTOs
{
    public class MenuReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MenuCreateDto
    {
        public string Name { get; set; } = null!;
    }

    public class MenuUpdateDto
    {
        public string Name { get; set; } = null!;
    }

}
