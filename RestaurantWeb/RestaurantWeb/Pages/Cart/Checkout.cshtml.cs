using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantWeb.Models;
using RestaurantWeb.Helpers;
using RestaurantAPI.DTOs;

namespace RestaurantWeb.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly OrderService _orderService;

        public List<CartItem> Cart { get; private set; } = [];

        public CheckoutModel(OrderService orderService)
        {
            _orderService = orderService;
        }

        public void OnGet()
        {
            Cart = HttpContext.Session.GetObject<List<CartItem>>("cart")
                   ?? new List<CartItem>();
        }

        // Когда нажали "Оформить"
        public async Task<IActionResult> OnPostAsync()
        {
            Cart = HttpContext.Session.GetObject<List<CartItem>>("cart")
                   ?? new List<CartItem>();

            if (Cart.Count == 0)
                return RedirectToPage("/Cart/Index");

            // Создание DTO
            var orderDto = new OrderCreateDto
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Пока нет авторизации
                Items = Cart.Select(c => new OrderItemCreateDto
                {
                    DishId = c.DishId,
                    Quantity = c.Quantity
                }).ToList()
            };

            // Отправка заказа на API
            var createdOrder = await _orderService.CreateOrderAsync(orderDto);

            // Очищаем корзину
            HttpContext.Session.Remove("cart");

            return RedirectToPage("/Orders/Details", new { id = createdOrder.Id });
        }
    }
}
