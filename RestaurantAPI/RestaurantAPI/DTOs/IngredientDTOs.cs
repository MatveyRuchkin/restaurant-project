using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class IngredientReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class IngredientCreateDto
    {
        [Required(ErrorMessage = "Название ингредиента обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;
    }

    public class IngredientUpdateDto
    {
        [Required(ErrorMessage = "Название ингредиента обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;
    }
}
