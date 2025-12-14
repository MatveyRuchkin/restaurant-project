using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantAPI.DTOs;
using RestaurantWeb.Helpers;
using RestaurantWeb.Models;

namespace RestaurantWeb.Pages.Menu
{
    public class IndexModel : PageModel
    {
        private readonly MenuService _menuService;
        private readonly DishService _dishService;

        public List<MenuReadDto> Menus { get; set; } = [];
        public List<DishReadDto> Dishes { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public Guid? SelectedMenuId { get; set; }

        // Получаем DishId из POST формы
        [BindProperty]
        public Guid DishId { get; set; }

        // Группировка блюд
        public Dictionary<string, List<DishReadDto>> GroupedDishes { get; set; } = new();

        public IndexModel(MenuService menuService, DishService dishService)
        {
            _menuService = menuService;
            _dishService = dishService;
        }

        // -------------------------------
        //  GET: Загрузка меню и блюд
        // -------------------------------
        public async Task OnGetAsync()
        {
            Menus = await _menuService.GetAllAsync();

            // 1. Ставим "Основное меню" первым
            Menus = Menus
                .OrderByDescending(m => m.Name == "Основное меню")
                .ThenBy(m => m.Name)
                .ToList();

            // 2. Если меню не выбрано — выбираем "Основное меню"
            if (!SelectedMenuId.HasValue)
            {
                var mainMenu = Menus.FirstOrDefault(m => m.Name == "Основное меню");
                if (mainMenu != null)
                    SelectedMenuId = mainMenu.Id;
            }

            // 3. Загрузка блюд
            if (SelectedMenuId.HasValue)
            {
                Dishes = await _dishService.GetDishesForMenuAsync(SelectedMenuId.Value);

                GroupedDishes = Dishes
                    .GroupBy(d => d.CategoryName)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key ?? "Без категории", g => g.ToList());
            }
        }

        // -------------------------------
        //  POST: Добавление блюда в корзину
        // -------------------------------
        public async Task<IActionResult> OnPostAddToCartAsync()
        {
            if (!SelectedMenuId.HasValue)
                return RedirectToPage();

            // Загружаем блюда выбранного меню
            var dishes = await _dishService.GetDishesForMenuAsync(SelectedMenuId.Value);
            var dish = dishes.FirstOrDefault(d => d.Id == DishId);

            if (dish == null)
                return RedirectToPage(new { SelectedMenuId });

            // Загружаем корзину из сессии
            var cart = HttpContext.Session.GetObject<List<CartItem>>("cart")
                       ?? new List<CartItem>();

            // Проверяем, есть ли блюдо
            var existing = cart.FirstOrDefault(c => c.DishId == DishId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    DishId = dish.Id,
                    DishName = dish.Name!,
                    Price = (double)dish.Price,
                    Quantity = 1
                });
            }

            // Сохраняем корзину обратно в сессию
            HttpContext.Session.SetObject("cart", cart);

            // Возврат на страницу с выбранным меню
            return RedirectToPage(new { SelectedMenuId });
        }
    }
}
