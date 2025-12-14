namespace RestaurantWeb.Models
{
    public class CartItem
    {
        public Guid DishId { get; set; }
        public string DishName { get; set; } = "";
        public double Price { get; set; }
        public int Quantity { get; set; }
    }
}
