using System;
using System.Collections.Generic;

namespace RestaurantAPI.Domain.Entities;

public partial class Cart
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

