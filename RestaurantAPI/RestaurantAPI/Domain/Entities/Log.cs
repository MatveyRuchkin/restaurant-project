using System;
using System.Collections.Generic;

namespace RestaurantAPI.Domain.Entities;

public partial class Log
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public Guid? RecordId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}

