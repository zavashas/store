// Controllers/Admin/AdminUserController.cs
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers.Admin
{
    public class AdminUserController : BaseAdminCrudController<User>
    {
        public AdminUserController(IHttpClientFactory httpClientFactory, AppLogger logger)
            : base(httpClientFactory, "Users", "User", logger) { }

        protected override async Task LoadViewBagsAsync()
        {
            var roles = await _httpClient.GetFromJsonAsync<List<Role>>("Roles") ?? new();
            ViewBag.Roles = roles;
        }

        [HttpGet]
        public override async Task<IActionResult> Index()
        {
            await LoadViewBagsAsync();
            var items = await _httpClient.GetFromJsonAsync<List<User>>(_endpoint) ?? new();
            return View("~/Views/Admin/Index.cshtml", items);
        }

        [HttpPost]
        public async Task<IActionResult> Add(User model)
        {
            if (!string.IsNullOrWhiteSpace(model.Email))
                model.Email = model.Email.Trim().ToLowerInvariant();

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(model, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<User>();
                TempData["UserSuccessMessage"] = "Пользователь успешно добавлен";
                return RedirectToAction("Edit", new { id = created?.IdUser });
            }

            await LoadViewBagsAsync();
            var errorText = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Ошибка при добавлении пользователя: {errorText}");
            return View($"~/Views/Admin/{_viewFolder}/{_viewFolder}.cshtml", model);
        }

        [HttpPost]
        public override async Task<IActionResult> Edit(User model, int id)
        {
            // Кто редактирует
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentUserIdStr, out var currentUserId);

            // Цель редактирования
            var existing = await _httpClient.GetFromJsonAsync<User>($"{_endpoint}/{id}");
            if (existing == null)
            {
                TempData["UserErrorMessage"] = "Пользователь не найден";
                return RedirectToAction("Index", "Admin");
            }

            // Нельзя редактировать Customer
            var targetRole = await _httpClient.GetFromJsonAsync<Role>($"Roles/{existing.RoleId}");
            if (targetRole?.Name?.Equals("Customer", StringComparison.OrdinalIgnoreCase) == true)
            {
                TempData["UserErrorMessage"] = "Редактирование пользователя с ролью Customer запрещено";
                return RedirectToAction("Index", "Admin");
            }

            // (Опционально) можно запретить редактировать самого себя через этот путь
            // если хочешь — раскомментируй:
            // if (currentUserId == id) {
            //     TempData["UserErrorMessage"] = "Нельзя редактировать самого себя из админки.";
            //     return RedirectToAction("Index", "Admin");
            // }

            // Пароль: если пришёл новый, иначе оставляем старый
            if (!string.IsNullOrWhiteSpace(model.PasswordHash) &&
                model.PasswordHash != existing.PasswordHash)
            {
                model.PasswordHash = model.PasswordHash; // API сам хэширует при необходимости
            }
            else
            {
                model.PasswordHash = existing.PasswordHash;
            }

            // Нормализуем email
            if (!string.IsNullOrWhiteSpace(model.Email))
                model.Email = model.Email.Trim().ToLowerInvariant();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_endpoint}/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["UserSuccessMessage"] = "Изменения сохранены";
                return RedirectToAction("Edit", new { id });
            }

            await LoadViewBagsAsync();
            TempData["UserErrorMessage"] = "Ошибка при редактировании пользователя";
            return View($"~/Views/Admin/User/User.cshtml", model);
        }

        [HttpPost]
        public override async Task<IActionResult> Delete(int id)
        {
            // Кто удаляет
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentUserIdStr, out var currentUserId);

            // Куда возвращаться
            var referer = Request.Headers["Referer"].ToString();
            IActionResult Back()
                => !string.IsNullOrWhiteSpace(referer)
                   ? Redirect(referer)                               
                   : RedirectToAction("Index", "AdminUser");         

            if (currentUserId == id)
            {
                TempData["UserErrorMessage"] = "Нельзя удалить самого себя.";
                return Back();
            }

            var user = await _httpClient.GetFromJsonAsync<User>($"{_endpoint}/{id}");
            if (user == null)
            {
                TempData["UserErrorMessage"] = "Пользователь не найден.";
                return Back();
            }

            var role = await _httpClient.GetFromJsonAsync<Role>($"Roles/{user.RoleId}");
            if (role?.Name?.Equals("Customer", StringComparison.OrdinalIgnoreCase) == true)
            {
                TempData["UserErrorMessage"] = "Удаление пользователя с ролью Customer запрещено.";
                return Back();
            }

            var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}");
            if (response.IsSuccessStatusCode)
                TempData["UserSuccessMessage"] = $"Пользователь ID={id} успешно удалён.";
            else
                TempData["UserErrorMessage"] = $"Ошибка при удалении пользователя ID={id}.";

            return Back();
        }

    }
}
