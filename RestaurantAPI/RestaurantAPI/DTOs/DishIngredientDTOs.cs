namespace RestaurantAPI.DTOs
{
    public class DishIngredientReadDto
    {
        public int Id { get; set; }
        public int DishId { get; set; }
        public string DishName { get; set; } = null!;
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string? Quantity { get; set; }
    }

    public class DishIngredientCreateDto
    {
        public int DishId { get; set; }
        public int IngredientId { get; set; }
        public string? Quantity { get; set; }
    }

    public class DishIngredientUpdateDto
    {
        public int IngredientId { get; set; }
        public string? Quantity { get; set; }
    }

}
