using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Controllers.Admin
{
    public class AdminProductController : BaseAdminCrudController<Product>
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;

        private const string ProductImagesRelative = "images/products";

        public AdminProductController(IHttpClientFactory httpClientFactory,
                                      IWebHostEnvironment env,
                                      AppLogger logger)

            : base(httpClientFactory, "Products", "Product", logger)
        {
            _httpClient = httpClientFactory.CreateClient("Api");
            _env = env;
        }

        [HttpGet]
        public override async Task<IActionResult> Index()
        {
            await LoadSelectListsAsync();
            var items = await _httpClient.GetFromJsonAsync<List<Product>>(_endpoint) ?? new();
            return View("~/Views/Admin/Index.cshtml", items);
        }

        private async Task LoadSelectListsAsync()
        {
            var categories = await _httpClient.GetFromJsonAsync<List<Category>>("Categories") ?? new();
            var manufacturers = await _httpClient.GetFromJsonAsync<List<Manufacturer>>("Manufacturers") ?? new();

            ViewBag.Categories = categories.Where(c => !c.Deleted).ToList();
            ViewBag.Manufacturers = manufacturers.Where(m => !m.Deleted).ToList();
            ViewBag.UnitOptions = new[] { "шт", "г", "кг", "мл", "л", "упаковка" }
                .Select(u => new SelectListItem { Text = u, Value = u })
                .ToList();
        }

        private string ProductImagesPhysical =>
            Path.Combine(_env.WebRootPath, ProductImagesRelative.Replace('/', Path.DirectorySeparatorChar));

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            try
            {
                Directory.CreateDirectory(ProductImagesPhysical);

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
                    throw new InvalidOperationException($"Недопустимый формат: {ext}");

                var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                var physicalPath = Path.Combine(ProductImagesPhysical, uniqueFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var relative = $"{ProductImagesRelative}/{uniqueFileName}".Replace("\\", "/");
                return relative;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet]
        public override async Task<IActionResult> Add()
        {
            await LoadSelectListsAsync();
            return View($"~/Views/Admin/Product/Product.cshtml", new Product());
        }

        [HttpGet]
        public override async Task<IActionResult> Edit(int id)
        {
            await LoadSelectListsAsync();

            var response = await _httpClient.GetAsync($"{_endpoint}/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var product = await response.Content.ReadFromJsonAsync<Product>();
            return View($"~/Views/Admin/Product/Product.cshtml", product);
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> Add(Product model, IFormFile? ImageFile)
        {
            var saved = await SaveImageAsync(ImageFile);
            if (!string.IsNullOrEmpty(saved))
                model.Image = saved;

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(model, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync(_endpoint, content);

            if (resp.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var errorText = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Ошибка при добавлении товара: {resp.StatusCode} — {errorText}");

            await LoadSelectListsAsync();
            return View($"~/Views/Admin/Product/Product.cshtml", model);
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> Update(Product model, IFormFile? ImageFile)
        {
            var existing = await _httpClient.GetFromJsonAsync<Product>($"{_endpoint}/{model.IdProduct}");
            if (existing == null) return NotFound();

            var saved = await SaveImageAsync(ImageFile);
            if (!string.IsNullOrEmpty(saved))
                model.Image = saved;
            else
                model.Image = existing.Image; 

            var resp = await _httpClient.PutAsJsonAsync($"{_endpoint}/{model.IdProduct}", model);
            if (resp.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Изменения сохранены";
                return RedirectToAction("Edit", new { id = model.IdProduct });
            }

            var err = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Ошибка сохранения товара: {err}");

            await LoadSelectListsAsync();
            return View($"~/Views/Admin/Product/Product.cshtml", model);
        }
    }
}
