using APISportFoodStore.Logging;
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly HttpClient _http;
        private readonly AppLogger _logger;

        public ProfileController(IHttpClientFactory factory, AppLogger logger)
        {
            _http = factory.CreateClient("Api");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Открыта страница профиля пользователя");

            var idStr = User.FindFirst("UserId")?.Value ?? "0";
            if (!int.TryParse(idStr, out var userId) || userId <= 0)
            {
                serilog.Warning("Не удалось определить ID текущего пользователя для профиля");
                return RedirectToAction("Authorization", "Account");
            }

            var user = await _http.GetFromJsonAsync<User>($"Users/{userId}");
            if (user == null)
            {
                serilog.Error("Не удалось загрузить профиль пользователя ID={Id}", userId);
                return View(new ProfileVm { Error = "Не удалось загрузить профиль." });
            }

            serilog.Information("Профиль пользователя ID={Id} успешно загружен", userId);

            return View(new ProfileVm
            {
                IdUser = user.IdUser ?? 0,
                Surname = user.Surname,
                Name = user.Name,
                MiddleName = user.MiddleName,
                Email = user.Email,
                Phone = user.Phone
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileVm vm)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Попытка обновить профиль пользователя");

            if (string.IsNullOrWhiteSpace(vm.Surname) || string.IsNullOrWhiteSpace(vm.Name) || string.IsNullOrWhiteSpace(vm.Email))
            {
                vm.Error = "Фамилия, имя и e-mail обязательны.";
                serilog.Warning("Ошибка валидации формы профиля — обязательные поля не заполнены");
                return View(vm);
            }

            var idStr = User.FindFirst("UserId")?.Value ?? "0";
            if (!int.TryParse(idStr, out var userId) || userId <= 0)
            {
                serilog.Warning("Не удалось определить ID текущего пользователя при сохранении профиля");
                return RedirectToAction("Authorization", "Account");
            }

            var current = await _http.GetFromJsonAsync<User>($"Users/{userId}");
            if (current == null)
            {
                vm.Error = "Не удалось загрузить профиль.";
                serilog.Error("Не удалось загрузить текущие данные пользователя ID={Id}", userId);
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                if (vm.NewPassword.Length < 6)
                {
                    vm.Error = "Новый пароль должен быть не короче 6 символов.";
                    serilog.Warning("Введён слишком короткий новый пароль пользователем ID={Id}", userId);
                    return View(vm);
                }

                if (string.IsNullOrWhiteSpace(vm.OldPassword))
                {
                    vm.Error = "Введите старый пароль для смены пароля.";
                    serilog.Warning("Не указан старый пароль при попытке смены пароля пользователем ID={Id}", userId);
                    return View(vm);
                }

                var authResp = await _http.PostAsJsonAsync("Users/authenticate", new
                {
                    username = (current.Email ?? string.Empty).Trim().ToLowerInvariant(),
                    password = vm.OldPassword
                });

                if (!authResp.IsSuccessStatusCode)
                {
                    vm.Error = "Старый пароль указан неверно.";
                    serilog.Warning("Неверно указан старый пароль пользователем ID={Id}", userId);
                    return View(vm);
                }
            }

            var dto = new User
            {
                IdUser = userId,
                Surname = (vm.Surname ?? current.Surname)?.Trim() ?? "",
                Name = (vm.Name ?? current.Name)?.Trim() ?? "",
                MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName.Trim(),
                Email = (vm.Email ?? current.Email)?.Trim().ToLowerInvariant() ?? "",
                Phone = (vm.Phone ?? current.Phone)?.Trim() ?? "",
                RoleId = current.RoleId,
                Deleted = current.Deleted,
                PasswordHash = !string.IsNullOrWhiteSpace(vm.NewPassword)
                    ? vm.NewPassword
                    : current.PasswordHash
            };

            var putResp = await _http.PutAsJsonAsync($"Users/{userId}", dto);
            if (!putResp.IsSuccessStatusCode)
            {
                string apiMsg = "";
                try { apiMsg = await putResp.Content.ReadAsStringAsync(); } catch { }

                if (putResp.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    vm.Error = "Пользователь с таким e-mail уже существует.";
                    serilog.Warning("Попытка сохранить профиль с существующим email пользователем ID={Id}", userId);
                }
                else if (putResp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    vm.Error = string.IsNullOrWhiteSpace(apiMsg)
                        ? "Ошибка запроса при сохранении профиля (проверьте корректность данных)."
                        : $"Ошибка запроса: {apiMsg}";
                    serilog.Error("Ошибка 400 при сохранении профиля ID={Id}: {Message}", userId, apiMsg);
                }
                else
                {
                    vm.Error = $"Ошибка сохранения профиля ({(int)putResp.StatusCode}). {apiMsg}";
                    serilog.Error("Неуспешный ответ {Code} при сохранении профиля ID={Id}: {Msg}",
                        putResp.StatusCode, userId, apiMsg);
                }

                return View(vm);
            }

            serilog.Information("Профиль пользователя ID={Id} успешно обновлён", userId);

            var updated = await _http.GetFromJsonAsync<User>($"Users/{userId}") ?? dto;

            var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Name, $"{updated.Name}".Trim()),
                new Claim(ClaimTypes.Email, updated.Email ?? dto.Email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, roleName)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            vm.Success = "Профиль успешно обновлён.";
            vm.IdUser = userId;
            vm.Email = updated.Email ?? vm.Email;
            vm.Surname = updated.Surname ?? vm.Surname;
            vm.Name = updated.Name ?? vm.Name;
            vm.MiddleName = updated.MiddleName;
            vm.Phone = updated.Phone ?? vm.Phone;
            vm.OldPassword = vm.NewPassword = null;

            serilog.Information("Сессия пользователя ID={Id} обновлена после сохранения профиля", userId);

            return View(vm);
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
    }

    public class ProfileVm
    {
        public int IdUser { get; set; }

        public string Surname { get; set; } = "";
        public string Name { get; set; } = "";
        public string? MiddleName { get; set; }
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }

        public string? Error { get; set; }
        public string? Success { get; set; }
    }
}
