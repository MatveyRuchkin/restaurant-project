using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class MenuDishReadDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuName { get; set; } = null!;
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
    }

    public class MenuDishCreateDto
    {
        [Required(ErrorMessage = "ID меню обязателен")]
        public Guid MenuId { get; set; }

        [Required(ErrorMessage = "ID блюда обязателен")]
        public Guid DishId { get; set; }
    }

    public class MenuDishUpdateDto
    {
        [Required(ErrorMessage = "ID меню обязателен")]
        public Guid MenuId { get; set; }

        [Required(ErrorMessage = "ID блюда обязателен")]
        public Guid DishId { get; set; }
    }
}
