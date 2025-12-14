using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class DishReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = null!;
    }

    public class DishCreateDto
    {
        [Required(ErrorMessage = "Название блюда обязательно")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 200 символов")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 999999.99, ErrorMessage = "Цена должна быть от 0.01 до 999999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Категория обязательна")]
        public Guid CategoryId { get; set; }
    }

    public class DishUpdateDto
    {
        [Required(ErrorMessage = "Название блюда обязательно")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 200 символов")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 999999.99, ErrorMessage = "Цена должна быть от 0.01 до 999999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Категория обязательна")]
        public Guid CategoryId { get; set; }
    }
}
