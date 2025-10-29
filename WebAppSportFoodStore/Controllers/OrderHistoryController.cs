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
    public class OrderHistoryController : Controller
    {
        private readonly HttpClient _http;
        private readonly AppLogger _logger;

        public OrderHistoryController(IHttpClientFactory factory, AppLogger logger)
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

        // GET: /OrderHistory
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            try
            {
                var orders = await _http.GetFromJsonAsync<List<OrderWithDetailsVm>>($"Orders/user/{uid}") ?? new();

                var vm = new OrderHistoryVm
                {
                    Orders = orders,
                    UserId = uid.Value
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Не удалось загрузить историю заказов.";
                return View(new OrderHistoryVm { Orders = new List<OrderWithDetailsVm>() });
            }
        }

        // GET: /OrderHistory/Details/5
        [HttpGet("Details/{orderId}")]
        public async Task<IActionResult> Details(int orderId)
        {
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            try
            {
                var order = await _http.GetFromJsonAsync<OrderWithDetailsVm>($"Orders/user/{uid}/details/{orderId}");

                if (order == null)
                {
                    TempData["Error"] = "Заказ не найден.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Не удалось загрузить детали заказа.";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    public class OrderHistoryVm
    {
        public int UserId { get; set; }
        public List<OrderWithDetailsVm> Orders { get; set; } = new();
    }

    public class OrderWithDetailsVm
    {
        public OrderVm Order { get; set; }
        public List<OrderDetailVm> OrderDetails { get; set; } = new();
        public string StatusName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderVm
    {
        public int? IdOrder { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DeliveryDate { get; set; }
        public int DeliverySlotId { get; set; }
        public int? OrderStatusId { get; set; }
        public string FormattedOrderDate => OrderDate.ToString("dd.MM.yyyy HH:mm");
        public string FormattedDeliveryDate => DeliveryDate.ToString("dd.MM.yyyy");
    }

    public class OrderDetailVm
    {
        public int? IdOrderDetail { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string ProductArticle { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string FormattedPrice => Price.ToString("N2");
        public string FormattedSubtotal => Subtotal.ToString("N2");
        public string ImageUrl => !string.IsNullOrEmpty(ProductImage) ? ProductImage : "/images/no-image.png";
    }
}