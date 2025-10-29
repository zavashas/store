using APISportFoodStore.Logging;
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly AppLogger _logger;

        public CartController(IHttpClientFactory factory, AppLogger logger)
        {
            _httpClient = factory.CreateClient("Api");
            _logger = logger;
        }

        private User GetCurrentUser()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idStr, out var id);
            return new User
            {
                IdUser = id,
                Name = User.Identity?.Name
            };
        }

        private bool TryGetUserId(out int userId)
        {
            var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(s, out userId);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Открыта корзина пользователя");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (userId <= 0)
            {
                serilog.Warning("Попытка открыть корзину без авторизации");
                return RedirectToAction("Authorization", "Account");
            }

            var response = await _httpClient.GetAsync("Carts");
            if (!response.IsSuccessStatusCode)
            {
                serilog.Error("Ошибка при получении корзины. Код: {Code}", response.StatusCode);
                return View(new List<Cart>());
            }

            var allItems = await response.Content.ReadFromJsonAsync<List<Cart>>() ?? new();
            var userItems = allItems.Where(c => c.UserId == userId).ToList();

            serilog.Information("Загружено {Count} элементов корзины для пользователя ID={UserId}", userItems.Count, userId);

            var productResponse = await _httpClient.GetAsync("Products");
            var products = await productResponse.Content.ReadFromJsonAsync<List<Product>>() ?? new();

            var cards = await _httpClient.GetFromJsonAsync<List<UserCard>>($"UserCards?userId={userId}");
            var addresses = await _httpClient.GetFromJsonAsync<List<UserAddress>>($"UserAddresses?userId={userId}");
            var slots = await _httpClient.GetFromJsonAsync<List<DeliveryTimeSlot>>("DeliveryTimeSlots");

            ViewBag.Products = products ?? new();
            ViewBag.Cards = cards?.Where(x => !x.Deleted).ToList() ?? new();
            ViewBag.Addresses = addresses?.Where(x => !x.Deleted).ToList() ?? new();
            ViewBag.Slots = slots ?? new();

            return View(userItems);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartId, int delta)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Изменение количества товара в корзине. CartId={CartId}, Delta={Delta}", cartId, delta);

            var response = await _httpClient.GetAsync($"Carts/{cartId}");
            if (!response.IsSuccessStatusCode)
            {
                serilog.Error("Не удалось загрузить элемент корзины ID={Id}. Код: {Code}", cartId, response.StatusCode);
                return BadRequest();
            }

            var item = await response.Content.ReadFromJsonAsync<Cart>();
            if (item == null)
            {
                serilog.Warning("Элемент корзины ID={Id} не найден", cartId);
                return NotFound();
            }

            item.Quantity += delta;
            if (item.Quantity < 1) item.Quantity = 1;

            var putResponse = await _httpClient.PutAsJsonAsync($"Carts/{cartId}", item);
            if (putResponse.IsSuccessStatusCode)
            {
                serilog.Information("Количество товара в корзине обновлено: CartId={CartId}, Quantity={Qty}", cartId, item.Quantity);
                return Ok();
            }

            serilog.Error("Ошибка при обновлении количества. CartId={CartId}, Код: {Code}", cartId, putResponse.StatusCode);
            return StatusCode((int)putResponse.StatusCode);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartId)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Удаление товара из корзины. CartId={CartId}", cartId);

            var response = await _httpClient.DeleteAsync($"Carts/{cartId}");
            if (response.IsSuccessStatusCode)
            {
                serilog.Information("Товар успешно удалён из корзины. CartId={CartId}", cartId);
                return Ok();
            }

            serilog.Error("Ошибка при удалении товара из корзины. CartId={CartId}, Код: {Code}", cartId, response.StatusCode);
            return StatusCode((int)response.StatusCode);
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            serilog.Information("Очистка корзины пользователя ID={UserId}", userId);

            var response = await _httpClient.GetAsync("Carts");
            var allItems = await response.Content.ReadFromJsonAsync<List<Cart>>() ?? new();
            var userItems = allItems.Where(c => c.UserId == userId);

            int deleted = 0;
            foreach (var item in userItems)
            {
                var del = await _httpClient.DeleteAsync($"Carts/{item.IdCart}");
                if (del.IsSuccessStatusCode) deleted++;
            }

            serilog.Information("Удалено {Count} элементов из корзины пользователя ID={UserId}", deleted, userId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            serilog.Information("Добавление товара в корзину: ProductId={ProductId}, UserId={UserId}", productId, userId);

            var cartItem = new { userId, productId, quantity = 1 };
            var response = await _httpClient.PostAsJsonAsync("Carts", cartItem);

            if (response.IsSuccessStatusCode)
            {
                serilog.Information("Товар ProductId={ProductId} успешно добавлен в корзину пользователя ID={UserId}", productId, userId);
                return Ok();
            }

            var msg = await response.Content.ReadAsStringAsync();
            serilog.Error("Ошибка при добавлении товара ProductId={ProductId} в корзину пользователя ID={UserId}. Код: {Code}, Сообщение: {Msg}",
                productId, userId, response.StatusCode, msg);
            return StatusCode((int)response.StatusCode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            var rawUid = User.FindFirst("UserId")?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(rawUid, out var userId))
            {
                serilog.Warning("Попытка добавить товар в корзину без авторизации. ProductId={ProductId}", productId);
                return Unauthorized();
            }

            if (quantity < 1) quantity = 1;

            var dto = new Cart
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity
            };

            serilog.Information("Добавление товара в корзину через форму. ProductId={ProductId}, Quantity={Qty}, UserId={UserId}",
                productId, quantity, userId);

            var apiResp = await _httpClient.PostAsJsonAsync("Carts", dto);
            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (!apiResp.IsSuccessStatusCode)
            {
                var msg = await apiResp.Content.ReadAsStringAsync();
                serilog.Error("Ошибка API при добавлении товара. ProductId={ProductId}, Код: {Code}, Сообщение: {Msg}",
                    productId, apiResp.StatusCode, msg);

                if (isAjax) return BadRequest(string.IsNullOrWhiteSpace(msg) ? "Не удалось добавить в корзину" : msg);
                TempData["CartError"] = msg;
                return RedirectToAction("Index", "Catalog");
            }

            serilog.Information("Товар ProductId={ProductId} успешно добавлен в корзину пользователя ID={UserId}", productId, userId);

            if (isAjax) return Ok();

            TempData["CartOk"] = "Товар добавлен в корзину";
            return RedirectToAction("Index", "Catalog");
        }
    }
}
