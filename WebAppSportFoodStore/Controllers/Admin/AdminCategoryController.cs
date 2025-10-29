using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers.Admin
{
    public class AdminCategoryController : BaseAdminCrudController<Category>
    {
        public AdminCategoryController(IHttpClientFactory httpClientFactory, AppLogger logger)
            : base(httpClientFactory, "Categories", "Category", logger) { }

        protected override async Task LoadViewBagsAsync()
        {
            var categories = await _httpClient.GetFromJsonAsync<List<Category>>("Categories") ?? new();
            ViewBag.ParentCategories = categories.Where(c => !c.Deleted).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Category model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var createdCategory = await response.Content.ReadFromJsonAsync<Category>();
                TempData["SuccessMessage"] = "Категория успешно добавлена";
                return RedirectToAction("Edit", new { id = createdCategory?.IdCategory });
            }

            await LoadViewBagsAsync();
            ModelState.AddModelError("", "Ошибка при добавлении категории");
            return View($"~/Admin/Views/Category/Category.cshtml", model);
        }

    }
}
