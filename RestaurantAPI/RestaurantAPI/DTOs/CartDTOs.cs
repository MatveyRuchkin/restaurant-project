using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.DTOs
{
    public class CartReadDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<CartItemReadDto> Items { get; set; } = new();
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
    }
    
    public class CartItemReadDto
    {
        public Guid Id { get; set; }
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
        public decimal DishPrice { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public decimal Subtotal { get; set; }
    }
    
    public class CartItemAddDto
    {
        [Required(ErrorMessage = "ID блюда обязателен")]
        public Guid DishId { get; set; }
        
        [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
        public int Quantity { get; set; } = 1;
        
        [StringLength(500, ErrorMessage = "Примечания не должны превышать 500 символов")]
        public string? Notes { get; set; }
    }
    
    public class CartItemUpdateDto
    {
        [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
        public int Quantity { get; set; }
        
        [StringLength(500, ErrorMessage = "Примечания не должны превышать 500 символов")]
        public string? Notes { get; set; }
    }
}



