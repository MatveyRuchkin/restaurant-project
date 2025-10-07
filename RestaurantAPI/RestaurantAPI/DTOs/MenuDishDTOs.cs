namespace RestaurantAPI.DTOs
{
    public class MenuDishReadDto
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public string MenuName { get; set; } = null!;
        public int DishId { get; set; }
        public string DishName { get; set; } = null!;
    }

    public class MenuDishCreateDto
    {
        public int MenuId { get; set; }
        public int DishId { get; set; }
    }

    public class MenuDishUpdateDto
    {
        public int MenuId { get; set; }
        public int DishId { get; set; }
    }

}
