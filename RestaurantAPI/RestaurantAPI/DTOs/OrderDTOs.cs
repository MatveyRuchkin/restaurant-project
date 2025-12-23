using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RestaurantAPI.Constants;

namespace RestaurantAPI.DTOs
{
    public class OrderReadDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        [JsonPropertyName("orderDate")]
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public decimal Total { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderCreateDto
    {
        [Required(ErrorMessage = "ID пользователя обязателен")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Список блюд обязателен")]
        [MinLength(1, ErrorMessage = "Заказ должен содержать хотя бы одно блюдо")]
        public List<OrderItemCreateDto> Items { get; set; } = new();

        [StringLength(1000, ErrorMessage = "Примечания к заказу не должны превышать 1000 символов")]
        public string? Notes { get; set; }

        [JsonPropertyName("orderDate")]
        public DateTime? OrderDate { get; set; } // Оставлено для обратной совместимости, но будет игнорироваться
    }

    public class OrderUpdateDto
    {
        [Required(ErrorMessage = "Статус обязателен")]
        [RegularExpression($"^({OrderStatuses.Pending}|{OrderStatuses.Processing}|{OrderStatuses.Completed}|{OrderStatuses.Cancelled})$", 
            ErrorMessage = $"Статус должен быть: {OrderStatuses.Pending}, {OrderStatuses.Processing}, {OrderStatuses.Completed} или {OrderStatuses.Cancelled}")]
        public string Status { get; set; } = null!;
    }
}
