using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class OrderItemReadDto
    {
        public Guid Id { get; set; }
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderItemCreateDto
    {
        [Required(ErrorMessage = "ID блюда обязателен")]
        public Guid DishId { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Примечания не должны превышать 500 символов")]
        public string? Notes { get; set; }
    }

    public class OrderItemUpdateDto
    {
        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
        public int Quantity { get; set; }
    }
}
