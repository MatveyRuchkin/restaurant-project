using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class MenuReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MenuCreateDto
    {
        [Required(ErrorMessage = "Название меню обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;
    }

    public class MenuUpdateDto
    {
        [Required(ErrorMessage = "Название меню обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        public string Name { get; set; } = null!;
    }
}
