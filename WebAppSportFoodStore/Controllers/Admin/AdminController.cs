using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Debugging;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using WebAppSportFoodStore.Models;
using AppLogger = APISportFoodStore.Logging.ILogger;

namespace WebAppSportFoodStore.Admin.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly AppLogger _logger;

        public AdminController(IHttpClientFactory httpClientFactory, AppLogger logger)
        {
            _httpClient = httpClientFactory.CreateClient("Api");
            _logger = logger;
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _httpClient.GetFromJsonAsync<List<Category>>("Categories");
            ViewBag.Manufacturers = await _httpClient.GetFromJsonAsync<List<Manufacturer>>("Manufacturers");
            ViewBag.Products = await _httpClient.GetFromJsonAsync<List<Product>>("Products");
            ViewBag.Users = await _httpClient.GetFromJsonAsync<List<User>>("Users");
            ViewBag.Orders = await _httpClient.GetFromJsonAsync<List<Order>>("Orders");
            ViewBag.DeliverySlots = await _httpClient.GetFromJsonAsync<List<DeliveryTimeSlot>>("DeliveryTimeSlots");

            ViewBag.CurrentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete(string entityType, int id)
        {
            var url = $"{entityType}/{id}";
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(new { Deleted = true })
            };

            await _httpClient.SendAsync(request);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Orders(string search, int? statusId)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());

            try
            {
                // Получаем заказы
                var ordersResponse = await _httpClient.GetAsync("Orders");
                if (!ordersResponse.IsSuccessStatusCode)
                {
                    serilog.Error("Error getting orders: {StatusCode}", ordersResponse.StatusCode);
                    TempData["ErrorMessage"] = "Ошибка загрузки заказов";
                    return View(new List<Order>());
                }

                var orders = await ordersResponse.Content.ReadFromJsonAsync<List<Order>>() ?? new List<Order>();

                serilog.Information("Loaded {Count} orders from API", orders.Count);

                // Логируем ID всех заказов для отладки
                foreach (var order in orders)
                {
                    serilog.Debug("Order ID: {OrderId}, UserId: {UserId}, Status: {StatusId}",
                        order.IdOrder, order.UserId, order.OrderStatusId);
                }

                var users = await _httpClient.GetFromJsonAsync<List<User>>("Users") ?? new();
                var slots = await _httpClient.GetFromJsonAsync<List<DeliveryTimeSlot>>("DeliveryTimeSlots") ?? new();

                // Получаем статусы заказов с обработкой ошибок
                List<OrderStatus> statuses = new();
                try
                {
                    statuses = await _httpClient.GetFromJsonAsync<List<OrderStatus>>("OrderStatus") ?? new();
                }
                catch (HttpRequestException ex)
                {
                    serilog.Warning(ex, "OrderStatuses endpoint not available, using default statuses");
                }

                // Применяем фильтры
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    orders = orders.Where(o =>
                        (o.IdOrder?.ToString() ?? "").Contains(search) ||
                        (o.UserId.ToString().Contains(search)) ||
                        users.Any(u => u.IdUser == o.UserId && u.Name?.ToLower().Contains(search) == true)
                    ).ToList();
                }

                if (statusId.HasValue)
                {
                    orders = orders.Where(o => o.OrderStatusId == statusId).ToList();
                }

                ViewBag.Statuses = statuses;
                ViewBag.Users = users;
                ViewBag.DeliverySlots = slots;
                ViewBag.Search = search;
                ViewBag.SelectedStatusId = statusId;

                return View(orders);
            }
            catch (Exception ex)
            {
                serilog.Error(ex, "Error loading orders page");
                TempData["ErrorMessage"] = "Ошибка загрузки страницы заказов";
                return View(new List<Order>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UpdateOrder(int id, int statusId, string deliveryDate, int deliverySlotId)
        {
            var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());

            serilog.Information("UpdateOrder form: id={Id}, statusId={StatusId}, deliveryDate={Date}, slotId={Slot}",
                id, statusId, deliveryDate, deliverySlotId);

            // Валидация и парсинг даты
            if (!DateOnly.TryParse(deliveryDate, out var parsedDeliveryDate))
            {
                serilog.Warning("Invalid delivery date format: {DeliveryDate}", deliveryDate);
                TempData["ErrorMessage"] = "Неверный формат даты доставки";
                return RedirectToAction("Orders");
            }

            // Формируем тело PUT — обязательно передаём все требуемые поля
            var orderUpdate = new Order
            {
                IdOrder = id,
                OrderStatusId = statusId,
                DeliveryDate = parsedDeliveryDate,
                DeliverySlotId = deliverySlotId
            };

            try
            {
                // Важно: BaseAddress у тебя уже с /api, поэтому здесь без префикса "api/"
                var response = await _httpClient.PutAsJsonAsync($"Orders/{id}", orderUpdate);

                if (response.IsSuccessStatusCode)
                {
                    serilog.Information("Order {OrderId} updated successfully", id);
                    TempData["SuccessMessage"] = "Изменения в заказе сохранены.";
                    return RedirectToAction("Orders");
                }

                var errorText = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 404)
                {
                    // API вернул NotFound, значит в БД реально нет такого заказа
                    serilog.Warning("Order {OrderId} not found on API. Body: {Body}", id, errorText);
                    TempData["ErrorMessage"] = $"Заказ не найден (ID: {id}).";
                }
                else if ((int)response.StatusCode == 400)
                {
                    // Например: дата в прошлом / неверный статус / ID в URL != ID в теле
                    serilog.Warning("BadRequest on update order {OrderId}: {Body}", id, errorText);
                    TempData["ErrorMessage"] = $"Ошибка в данных: {errorText}";
                }
                else
                {
                    serilog.Error("Error updating order {OrderId}: {Status} {Body}", id, response.StatusCode, errorText);
                    TempData["ErrorMessage"] = $"Ошибка обновления заказа: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                serilog.Error(ex, "Exception while updating order {OrderId}", id);
                TempData["ErrorMessage"] = $"Внутренняя ошибка при обновлении заказа: {ex.Message}";
            }

            return RedirectToAction("Orders");
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var orderDetails = await _httpClient.GetFromJsonAsync<List<OrderDetail>>($"OrderDetails") ?? new();
            var products = await _httpClient.GetFromJsonAsync<List<Product>>("Products") ?? new();

            var details = orderDetails
                .Where(od => od.OrderId == orderId)
                .Join(products,
                    od => od.ProductId,
                    p => p.IdProduct,
                    (od, p) => new
                    {
                        ProductName = p.Name,
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Subtotal = od.Quantity * od.Price
                    })
                .ToList();

            return Json(details);
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


[HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Statistics(DateTime? from = null, DateTime? to = null, string groupBy = "day", int topN = 10)
    {
        var serilog = _logger.GetForSession(HttpContext, GetCurrentUser());

        // Диапазон по умолчанию: последние 30 дней
        var end = to?.Date ?? DateTime.Today;
        var start = from?.Date ?? end.AddDays(-29);

        try
        {
            // Берём всё разом
            var orders = await _httpClient.GetFromJsonAsync<List<Order>>("Orders") ?? new();
            var orderDetails = await _httpClient.GetFromJsonAsync<List<OrderDetail>>("OrderDetails") ?? new();
            var products = await _httpClient.GetFromJsonAsync<List<Product>>("Products") ?? new();
            var users = await _httpClient.GetFromJsonAsync<List<User>>("Users") ?? new();

            // Фильтруем по диапазону
            var periodOrders = orders
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end)
                .ToList();

            // --- KPI
            var totalRevenue = periodOrders.Sum(o => o.TotalAmount);
            var totalOrders = periodOrders.Count;
            var uniqueCustomers = periodOrders.Select(o => o.UserId).Distinct().Count();

            // --- Группировка: "day" или "month"
            bool byMonth = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase);

            IEnumerable<(string Label, IEnumerable<Order> Group)> orderGroups;
            if (byMonth)
            {
                orderGroups = periodOrders
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g =>
                    {
                        var label = new DateTime(g.Key.Year, g.Key.Month, 1)
                            .ToString("MMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
                        return (label, g.AsEnumerable());
                    });
            }
            else
            {
                orderGroups = Enumerable.Range(0, (end - start).Days + 1)
                    .Select(i => start.AddDays(i))
                    .Select(d =>
                    {
                        var dayOrders = periodOrders.Where(o => o.OrderDate.Date == d.Date);
                        var label = d.ToString("dd.MM", CultureInfo.GetCultureInfo("ru-RU"));
                        return (label, dayOrders);
                    });
            }

            var ordersLabels = orderGroups.Select(g => g.Label).ToList();
            var ordersCounts = orderGroups.Select(g => g.Group.Count()).ToList();
            var revenueValues = orderGroups.Select(g => g.Group.Sum(x => x.TotalAmount)).ToList();

            // --- Топ популярных товаров (по количеству штук в деталях)
            var orderIdsInPeriod = periodOrders.Select(o => o.IdOrder).ToHashSet();
            var topProducts = orderDetails
                .Where(d => orderIdsInPeriod.Contains(d.OrderId))
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(topN)
                .ToList();

            var productById = products.ToDictionary(p => p.IdProduct, p => p.Name ?? $"Товар #{p.IdProduct}");
            var topProductsLabels = topProducts.Select(x => productById.TryGetValue(x.ProductId, out var n) ? n : $"#{x.ProductId}").ToList();
            var topProductsQty = topProducts.Select(x => x.Qty).ToList();

            var vm = new AdminStatsVm
            {
                PeriodTitle = byMonth
                    ? $"{start:MMM yyyy} — {end:MMM yyyy}"
                    : $"{start:dd.MM.yyyy} — {end:dd.MM.yyyy}",
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                UniqueCustomers = uniqueCustomers,
                OrdersLabels = ordersLabels,
                OrdersCounts = ordersCounts,
                RevenueLabels = ordersLabels,          // те же метки, что и для заказов
                RevenueValues = revenueValues,
                TopProductsLabels = topProductsLabels,
                TopProductsQty = topProductsQty,
                GroupBy = byMonth ? "month" : "day",
                TopN = topN
            };

            return View("Statistics", vm);
        }
        catch (Exception ex)
        {
            serilog.Error(ex, "Error calculating admin statistics");
            TempData["ErrorMessage"] = "Не удалось загрузить статистику";
            return View("Statistics", new AdminStatsVm());
        }
    }

    // Вспомогательная VM (можно в отдельный файл Models/ViewModels/AdminStatsVm.cs)
    

}
}