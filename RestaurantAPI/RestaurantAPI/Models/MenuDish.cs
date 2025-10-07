using System;
using System.Collections.Generic;

namespace RestaurantAPI.Models;

public partial class MenuDish
{
    public int Id { get; set; }

    public int MenuId { get; set; }

    public int DishId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual Menu Menu { get; set; } = null!;
}
