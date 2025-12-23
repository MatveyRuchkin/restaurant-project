using System;

namespace RestaurantAPI.Models;

public partial class CartItem
{
    public Guid Id { get; set; }
    
    public Guid CartId { get; set; }
    
    public Guid DishId { get; set; }
    
    public int Quantity { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Навигационные свойства
    public virtual Cart Cart { get; set; } = null!;
    public virtual Dish Dish { get; set; } = null!;
}

