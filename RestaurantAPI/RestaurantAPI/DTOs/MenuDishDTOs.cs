namespace RestaurantAPI.DTOs
{
    public class MenuDishReadDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuName { get; set; } = null!;
        public Guid DishId { get; set; }
        public string DishName { get; set; } = null!;
    }

    public class MenuDishCreateDto
    {
        public Guid MenuId { get; set; }
        public Guid DishId { get; set; }
    }

    public class MenuDishUpdateDto
    {
        public Guid MenuId { get; set; }
        public Guid DishId { get; set; }
    }

}
