namespace RestaurantApi.DTOs
{
    public class IngredientReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class IngredientCreateDto
    {
        public string Name { get; set; } = null!;
    }

    public class IngredientUpdateDto
    {
        public string Name { get; set; } = null!;
    }

}
