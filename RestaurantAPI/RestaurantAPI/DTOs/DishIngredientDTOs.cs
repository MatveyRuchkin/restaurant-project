using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "ID блюда обязателен")]
        public Guid DishId { get; set; }

        [Required(ErrorMessage = "ID ингредиента обязателен")]
        public Guid IngredientId { get; set; }

        [StringLength(50, ErrorMessage = "Количество не должно превышать 50 символов")]
        public string? Quantity { get; set; }
    }

    public class DishIngredientUpdateDto
    {
        [Required(ErrorMessage = "ID ингредиента обязателен")]
        public Guid IngredientId { get; set; }

        [StringLength(50, ErrorMessage = "Количество не должно превышать 50 символов")]
        public string? Quantity { get; set; }
    }
}
