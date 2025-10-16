using System;
using System.Collections.Generic;

namespace RestaurantAPI.Models;

public partial class Menu
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<MenuDish> MenuDishes { get; set; } = new List<MenuDish>();
}
