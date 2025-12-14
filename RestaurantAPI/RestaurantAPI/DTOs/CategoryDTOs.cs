using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Примечания не должны превышать 500 символов")]
        public string? Notes { get; set; }
    }

    public class CategoryUpdateDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Примечания не должны превышать 500 символов")]
        public string? Notes { get; set; }
    }
}
