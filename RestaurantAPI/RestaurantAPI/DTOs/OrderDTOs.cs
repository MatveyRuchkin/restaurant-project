using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class OrderReadDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal Total { get; set; }
    }

    public class OrderCreateDto
    {
        [Required(ErrorMessage = "ID пользователя обязателен")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Список блюд обязателен")]
        [MinLength(1, ErrorMessage = "Заказ должен содержать хотя бы одно блюдо")]
        public List<OrderItemCreateDto> Items { get; set; } = new();
    }

    public class OrderUpdateDto
    {
        [Required(ErrorMessage = "Статус обязателен")]
        [RegularExpression("^(Pending|Processing|Completed|Cancelled)$", 
            ErrorMessage = "Статус должен быть: Pending, Processing, Completed или Cancelled")]
        public string Status { get; set; } = null!;
    }
}
