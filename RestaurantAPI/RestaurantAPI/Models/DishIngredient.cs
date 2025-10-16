using System;
using System.Collections.Generic;

namespace RestaurantAPI.Models;

public partial class DishIngredient
{
    public Guid Id { get; set; }

    public Guid DishId { get; set; }

    public Guid IngredientId { get; set; }

    public string? Quantity { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual Ingredient Ingredient { get; set; } = null!;
}
