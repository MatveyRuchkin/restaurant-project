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
        public Guid UserId { get; set; }
        public List<OrderItemCreateDto> Items { get; set; } = new();
    }

    public class OrderUpdateDto
    {
        public string Status { get; set; } = null!;
    }

}
