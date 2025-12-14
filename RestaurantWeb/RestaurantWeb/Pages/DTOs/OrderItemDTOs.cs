namespace RestaurantAPI.DTOs
{
    public class OrderItemReadDto
    {
        public Guid Id { get; set; }
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderItemCreateDto
    {
        public Guid DishId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderItemUpdateDto
    {
        public int Quantity { get; set; }
    }

}
