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
    public class AddressesController : Controller
    {
        private readonly HttpClient _http;
        private readonly AppLogger _logger;

        public AddressesController(IHttpClientFactory factory, AppLogger logger)
        {
            _http = factory.CreateClient("Api");
            _logger = logger;
        }

        private int? GetUid()
        {
            var s = User.FindFirst("UserId")?.Value
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(s, out var uid) ? uid : null;
        }

        private User GetCurrentUser()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idStr, out var id);
            return new User { IdUser = id, Name = User.Identity?.Name };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null)
            {
                log.Warning("Попытка открыть адреса без авторизации");
                return RedirectToAction("Authorization", "Account");
            }

            try
            {
                // если API поддерживает ?userId= — используем как в других местах проекта
                var items = await _http.GetFromJsonAsync<List<UserAddress>>($"UserAddresses?userId={uid}")
                         ?? new List<UserAddress>();

                items = items
                    .Where(a => !a.Deleted && a.UserId == uid)
                    .OrderByDescending(a => a.IdAddress ?? 0)
                    .ToList();

                log.Information("Загружено {Count} адресов для пользователя {UserId}", items.Count, uid);
                return View(items);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Ошибка загрузки адресов пользователя {UserId}", uid);
                return View(new List<UserAddress>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            if (id is null)
            {
                log.Information("Открыта форма добавления адреса для пользователя {UserId}", uid);
                return View(new UserAddress { UserId = uid.Value, City = "", Street = "", House = "" });
            }

            var item = await _http.GetFromJsonAsync<UserAddress>($"UserAddresses/{id}");
            if (item == null || item.UserId != uid)
            {
                log.Warning("Попытка открыть чужой адрес. AddressId={Id}, UserId={UserId}", id, uid);
                return NotFound();
            }

            log.Information("Открыта форма редактирования адреса {Id} для пользователя {UserId}", id, uid);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserAddress model)
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            if (string.IsNullOrWhiteSpace(model.City) ||
                string.IsNullOrWhiteSpace(model.Street) ||
                string.IsNullOrWhiteSpace(model.House))
            {
                ViewBag.Error = "Город, улица и дом — обязательны.";
                log.Warning("Ошибка валидации адреса: отсутствуют обязательные поля, UserId={UserId}", uid);
                return View(model);
            }

            model.UserId = uid.Value;
            model.Deleted = false;

            HttpResponseMessage resp;
            if ((model.IdAddress ?? 0) == 0)
            {
                log.Information("Добавление нового адреса пользователем {UserId}", uid);
                resp = await _http.PostAsJsonAsync("UserAddresses", model);
            }
            else
            {
                log.Information("Сохранение адреса {Id} пользователем {UserId}", model.IdAddress, uid);
                resp = await _http.PutAsJsonAsync($"UserAddresses/{model.IdAddress}", model);
            }

            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                ViewBag.Error = string.IsNullOrWhiteSpace(msg) ? "Не удалось сохранить адрес." : msg;
                log.Error("Ошибка API при сохранении адреса. Код={Code}, Msg={Msg}", resp.StatusCode, msg);
                return View(model);
            }

            TempData["Ok"] = "Адрес сохранён.";
            log.Information("Адрес успешно сохранён. AddressId={Id}, UserId={UserId}", model.IdAddress, uid);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null) return Unauthorized();

            log.Information("Запрос на удаление адреса {Id} пользователем {UserId}", id, uid);

            var item = await _http.GetFromJsonAsync<UserAddress>($"UserAddresses/{id}");
            if (item == null || item.UserId != uid) return NotFound();

            item.Deleted = true;
            var resp = await _http.PutAsJsonAsync($"UserAddresses/{id}", item);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                log.Error("Не удалось удалить адрес {Id}. Код={Code}, Msg={Msg}", id, resp.StatusCode, msg);
                return BadRequest(string.IsNullOrWhiteSpace(msg) ? "Не удалось удалить адрес." : msg);
            }

            log.Information("Адрес {Id} помечен как удалённый пользователем {UserId}", id, uid);
            return RedirectToAction(nameof(Index));
        }
    }
}
