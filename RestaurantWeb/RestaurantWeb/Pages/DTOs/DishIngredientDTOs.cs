namespace RestaurantAPI.DTOs
{
    public class DishIngredientReadDto
    {
        public Guid Id { get; set; }
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string? Quantity { get; set; }
    }

    public class DishIngredientCreateDto
    {
        public Guid DishId { get; set; }
        public Guid IngredientId { get; set; }
        public string? Quantity { get; set; }
    }

    public class DishIngredientUpdateDto
    {
        public Guid IngredientId { get; set; }
        public string? Quantity { get; set; }
    }

}
