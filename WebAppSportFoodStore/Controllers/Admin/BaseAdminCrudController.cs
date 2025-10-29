using APISportFoodStore.Logging;
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers.Admin
{
    /// <summary>
    /// базовый контроллер для CRUD операций 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseAdminCrudController<T> : Controller where T : class
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _endpoint;
        protected readonly string _viewFolder;
        private readonly AppLogger _logger;

        protected BaseAdminCrudController(
            IHttpClientFactory httpClientFactory,
            string endpoint,
            string viewFolder,
            AppLogger logger)
        {
            _httpClient = httpClientFactory.CreateClient("Api");
            _endpoint = endpoint;
            _viewFolder = viewFolder;
            _logger = logger;
        }

        protected virtual Task LoadViewBagsAsync() => Task.CompletedTask;

        public virtual async Task<IActionResult> Index()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Открыт список элементов для {Entity}", typeof(T).Name);

            var data = await _httpClient.GetFromJsonAsync<List<T>>(_endpoint) ?? new();
            return View($"~/Views/Admin/Index.cshtml", data);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Add()
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Открыта форма добавления сущности {Entity}", typeof(T).Name);

            await LoadViewBagsAsync();
            return View($"~/Views/Admin/{_viewFolder}/{_viewFolder}.cshtml", Activator.CreateInstance<T>());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(int id)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Открыта форма редактирования {Entity} с ID={Id}", typeof(T).Name, id);

            var item = await _httpClient.GetFromJsonAsync<T>($"{_endpoint}/{id}");
            if (item == null)
            {
                serilog.Warning("Сущность {Entity} с ID={Id} не найдена", typeof(T).Name, id);
                return NotFound();
            }

            await LoadViewBagsAsync();
            return View($"~/Views/Admin/{_viewFolder}/{_viewFolder}.cshtml", item);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit(T model, int id)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Попытка сохранить изменения для {Entity} ID={Id}", typeof(T).Name, id);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_endpoint}/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                serilog.Information("Изменения для {Entity} ID={Id} успешно сохранены", typeof(T).Name, id);
                TempData["SuccessMessage"] = "Изменения сохранены.";
                return RedirectToAction("Edit", new { id });
            }

            serilog.Error("Ошибка при сохранении {Entity} ID={Id}. Код ответа: {Code}", typeof(T).Name, id, response.StatusCode);
            await LoadViewBagsAsync();
            ModelState.AddModelError("", "Ошибка при редактировании");
            return View($"~/Views/Admin/{_viewFolder}/{_viewFolder}.cshtml", model);
        }

        /// <summary>
        /// soft удаление
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());
            serilog.Information("Удаление {Entity} с ID={Id}", typeof(T).Name, id);

            var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}");
            if (response.IsSuccessStatusCode)
            {
                serilog.Information("{Entity} с ID={Id} успешно удалён", typeof(T).Name, id);
            }
            else
            {
                serilog.Error("Ошибка при удалении {Entity} с ID={Id}. Код ответа: {Code}", typeof(T).Name, id, response.StatusCode);
            }

            return RedirectToAction("Index", "Admin");
        }

        protected object GetEntityId(T entity)
        {
            var idProp = typeof(T).GetProperties().FirstOrDefault(p => p.Name.StartsWith("Id"));
            return idProp?.GetValue(entity) ?? 0;
        }

        /// <summary>
        /// Получение текущего пользователя 
        /// </summary>
        protected virtual User GetCurrentUser()
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
}
