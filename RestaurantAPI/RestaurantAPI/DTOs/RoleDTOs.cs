using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class RoleReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class RoleCreateDto
    {
        [Required(ErrorMessage = "Название роли обязательно")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 50 символов")]
        public string Name { get; set; } = null!;
    }

    public class RoleUpdateDto
    {
        [Required(ErrorMessage = "Название роли обязательно")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 50 символов")]
        public string Name { get; set; } = null!;
    }
}
