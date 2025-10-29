using APISportFoodStore.Logging;
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly HttpClient _http;
        private readonly AppLogger _logger;

        public FavoritesController(IHttpClientFactory factory, AppLogger logger)
        {
            _http = factory.CreateClient("Api");
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

        private int? GetUid()
        {
            var s = User.FindFirst("UserId")?.Value
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(s, out var uid) ? uid : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null)
            {
                serilog.Warning("Попытка открыть избранное без авторизации");
                return RedirectToAction("Authorization", "Account");
            }

            serilog.Information("Открыта страница избранного для пользователя ID={UserId}", uid);

            HashSet<int> favIds = new();

            try
            {
                var ids = await _http.GetFromJsonAsync<List<int>>($"Favorites/by-user/{uid}");
                if (ids != null && ids.Count > 0)
                {
                    favIds = ids.ToHashSet();
                    serilog.Information("Избранные товары получены через API (список id): {Count} элементов", favIds.Count);
                }
                else
                {
                    var favs = await _http.GetFromJsonAsync<List<Favorite>>($"Favorites/by-user/{uid}") ?? new();
                    favIds = favs.Select(f => f.ProductId).ToHashSet();
                    serilog.Information("Избранные товары получены через API (объекты): {Count} элементов", favIds.Count);
                }
            }
            catch (Exception ex)
            {
                serilog.Error(ex, "Ошибка при получении избранных товаров для пользователя ID={UserId}", uid);
                return View(Enumerable.Empty<ProductDto>());
            }

            ViewBag.FavoriteProductIds = favIds;

            if (favIds.Count == 0)
            {
                serilog.Information("У пользователя ID={UserId} нет избранных товаров", uid);
                return View(Enumerable.Empty<ProductDto>());
            }

            try
            {
                var allPublic = await _http.GetFromJsonAsync<List<ProductDto>>("products/public") ?? new();
                var onlyFavorites = allPublic.Where(p => favIds.Contains(p.IdProduct)).ToList();

                serilog.Information("Загружено {Count} избранных товаров для пользователя ID={UserId}", onlyFavorites.Count, uid);
                return View(onlyFavorites);
            }
            catch (Exception ex)
            {
                serilog.Error(ex, "Ошибка при загрузке списка продуктов для избранного пользователя ID={UserId}", uid);
                return View(Enumerable.Empty<ProductDto>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle([FromForm] int productId)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());

            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("UserId")?.Value;

            if (!int.TryParse(idStr, out var userId))
            {
                serilog.Warning("Попытка переключить избранное без авторизации. ProductId={ProductId}", productId);
                return Unauthorized();
            }

            serilog.Information("Переключение состояния избранного. UserId={UserId}, ProductId={ProductId}", userId, productId);

            try
            {
                var resp = await _http.PostAsJsonAsync("favorites/toggle", new { UserId = userId, ProductId = productId });

                if (!resp.IsSuccessStatusCode)
                {
                    var msg = await resp.Content.ReadAsStringAsync();
                    serilog.Error("Ошибка API при переключении избранного. UserId={UserId}, ProductId={ProductId}, Код={Code}, Сообщение={Msg}",
                        userId, productId, resp.StatusCode, msg);
                    return BadRequest(string.IsNullOrWhiteSpace(msg) ? "Ошибка при добавлении в избранное" : msg);
                }

                var dto = await resp.Content.ReadFromJsonAsync<ToggleResult>();

                return Json(new { ok = true, inFavorites = dto?.inFavorites ?? false });
            }
            catch (Exception ex)
            {
                serilog.Error(ex, "Исключение при переключении избранного. UserId={UserId}, ProductId={ProductId}", userId, productId);
                return BadRequest("Ошибка при обработке запроса избранного");
            }
        }

        private record ToggleResult(bool inFavorites);
    }
}
