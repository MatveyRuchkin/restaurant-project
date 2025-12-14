using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantWeb.Helpers;
using RestaurantWeb.Models;

namespace RestaurantWeb.Pages.Cart
{
    public class IndexModel : PageModel
    {
        public List<CartItem> Cart { get; set; } = [];

        public void OnGet()
        {
            Cart = HttpContext.Session.GetObject<List<CartItem>>("cart")
                   ?? new List<CartItem>();
        }

        public IActionResult OnPostClear()
        {
            HttpContext.Session.Remove("cart");
            return RedirectToPage();
        }
    }
}
