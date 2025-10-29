using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminManufacturerController : BaseAdminCrudController<Manufacturer>
    {
        public AdminManufacturerController(IHttpClientFactory httpClientFactory, AppLogger logger)
            : base(httpClientFactory, "Manufacturers", "Manufacturer", logger) { }

        [HttpPost]
        public async Task<IActionResult> Add(Manufacturer model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Manufacturer>();
                TempData["SuccessMessage"] = "Производитель успешно добавлен";
                return RedirectToAction("Edit", new { id = created?.IdManufacturer });
            }

            await LoadViewBagsAsync();
            ModelState.AddModelError("", "Ошибка при добавлении производителя");
            return View($"~/Views/Admin/Manufacturer/Manufacturer.cshtml", model);
        }
    }
}
