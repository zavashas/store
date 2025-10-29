using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebAppSportFoodStore.Controllers
{
    public class CatalogController : Controller
    {
        private readonly HttpClient _http;

        public CatalogController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("Api");
        }
        private int? GetCurrentUserId()
        {
            var raw =
                User.FindFirst("UserId")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(raw, out var uid) ? uid : null;
        }

        public async Task<IActionResult> Index()
        {
            // Справочники
            var manufacturers = await SafeGet<List<Manufacturer>>("Manufacturers") ?? new();
            var categories = await SafeGet<List<Category>>("Categories") ?? new();

            ViewBag.Manufacturers = manufacturers.Where(m => !m.Deleted).ToList();
            ViewBag.Categories = categories.Where(c => !c.Deleted).ToList();

            // Избранное + корзина
            var uid = GetCurrentUserId();
            if (uid is not null)
            {
                var favIds = await SafeGet<List<int>>($"Favorites/by-user/{uid}") ?? new();
                ViewBag.FavoriteProductIds = favIds.ToHashSet();

                var cartItems = await SafeGet<List<Cart>>($"Carts/User/{uid}") ?? new();
                ViewBag.CartProductIds = cartItems.Select(x => x.ProductId).ToHashSet();
            }
            else
            {
                ViewBag.FavoriteProductIds = new HashSet<int>();
                ViewBag.CartProductIds = new HashSet<int>();
            }

            var list = await SafeGet<List<ProductDto>>("products/public") ?? new();
            return View(list);
        }

        /// <summary>
        /// Детали товара
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var item = await SafeGet<ProductDto>($"products/public/{id}");
            if (item is null) return NotFound();

            var uid = GetCurrentUserId();
            if (uid is not null)
            {
                var favIds = await SafeGet<List<int>>($"Favorites/by-user/{uid}") ?? new();
                ViewBag.FavoriteProductIds = favIds.ToHashSet();

                var cartItems = await SafeGet<List<Cart>>($"Carts/User/{uid}") ?? new();
                ViewBag.CartProductIds = cartItems.Select(c => c.ProductId).ToHashSet();
            }
            else
            {
                ViewBag.FavoriteProductIds = new HashSet<int>();
                ViewBag.CartProductIds = new HashSet<int>();
            }

            return View(item);
        }

        private async Task<T?> SafeGet<T>(string relativeUrl)
        {
            try
            {
                var resp = await _http.GetAsync(relativeUrl);
                if (!resp.IsSuccessStatusCode) return default;
                return await resp.Content.ReadFromJsonAsync<T>();
            }
            catch
            {
                return default;
            }
        }
    }
}
