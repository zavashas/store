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
    public class CheckoutController : Controller
    {
        private readonly HttpClient _http;
        private readonly AppLogger _logger;

        public CheckoutController(IHttpClientFactory factory, AppLogger logger)
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

        // GET /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            // тянем корзину, продукты, адреса, слоты
            var carts = await _http.GetFromJsonAsync<List<Cart>>("Carts") ?? new();
            var userCart = carts.Where(c => c.UserId == uid).ToList();

            if (userCart.Count == 0)
            {
                TempData["CartError"] = "Корзина пуста.";
                return RedirectToAction("Index", "Cart");
            }

            var products = await _http.GetFromJsonAsync<List<Product>>("Products") ?? new();
            var addresses = await _http.GetFromJsonAsync<List<UserAddress>>($"UserAddresses?userId={uid}") ?? new();
            var slots = await _http.GetFromJsonAsync<List<DeliveryTimeSlot>>("DeliveryTimeSlots") ?? new();

            addresses = addresses.Where(a => !a.Deleted && a.UserId == uid).ToList();

            // считаем итог
            decimal total = 0;
            foreach (var line in userCart)
            {
                var p = products.FirstOrDefault(x => x.IdProduct == line.ProductId);
                if (p != null) total += p.Price * line.Quantity;
            }

            // Рассчитываем минимальную дату доставки (завтра)
            var minDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

            var vm = new CheckoutVm
            {
                UserId = uid.Value,
                Lines = userCart.Select(c =>
                {
                    var p = products.FirstOrDefault(x => x.IdProduct == c.ProductId);
                    return new CheckoutLine
                    {
                        CartId = c.IdCart ?? 0,
                        ProductId = c.ProductId,
                        Name = p?.Name ?? $"Товар #{c.ProductId}",
                        Price = p?.Price ?? 0,
                        Quantity = c.Quantity,
                        Subtotal = (p?.Price ?? 0) * c.Quantity
                    };
                }).ToList(),
                Addresses = addresses,
                Slots = slots,
                Total = total,
                MinDeliveryDate = minDeliveryDate,
                DefaultDeliveryDate = minDeliveryDate // По умолчанию - следующий день
            };

            log.Information("Открыта страница оформления заказа. UserId={UserId}, Items={Count}, Total={Total}",
                uid, vm.Lines.Count, vm.Total);

            return View(vm);
        }

        // POST /Checkout/Place
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(PlaceOrderDto dto)
        {
            var log = _logger.GetForSession(HttpContext, GetCurrentUser());
            var uid = GetUid();
            if (uid is null) return RedirectToAction("Authorization", "Account");

            if (dto.AddressId <= 0 || dto.DeliverySlotId <= 0 || dto.DeliveryDate == default)
            {
                TempData["OrderError"] = "Выберите адрес, дату и время доставки.";
                return RedirectToAction(nameof(Index));
            }

            // Проверяем, что дата доставки не раньше чем через день
            var minDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            if (dto.DeliveryDate < minDeliveryDate)
            {
                TempData["OrderError"] = "Дата доставки должна быть не ранее завтрашнего дня.";
                return RedirectToAction(nameof(Index));
            }

            // Остальной код метода Place остается без изменений...
            // Подтягиваем корзину и продукты
            var carts = await _http.GetFromJsonAsync<List<Cart>>("Carts") ?? new();
            var products = await _http.GetFromJsonAsync<List<Product>>("Products") ?? new();
            var userCart = carts.Where(c => c.UserId == uid).ToList();

            if (userCart.Count == 0)
            {
                TempData["OrderError"] = "Корзина пуста.";
                return RedirectToAction("Index", "Cart");
            }

            // Рассчитываем общую сумму и проверяем наличие товаров
            decimal total = 0m;
            var orderItems = new List<OrderItemDto>();
            var productUpdates = new Dictionary<int, int>(); // ProductId -> Quantity to deduct

            foreach (var line in userCart)
            {
                var p = products.FirstOrDefault(x => x.IdProduct == line.ProductId);
                if (p == null)
                {
                    TempData["OrderError"] = $"Товар #{line.ProductId} не найден.";
                    return RedirectToAction(nameof(Index));
                }

                if (p.Quantity < line.Quantity)
                {
                    TempData["OrderError"] = $"Недостаточно товара '{p.Name}'. В наличии: {p.Quantity}, запрошено: {line.Quantity}";
                    return RedirectToAction(nameof(Index));
                }

                total += p.Price * line.Quantity;
                orderItems.Add(new OrderItemDto
                {
                    ProductId = p.IdProduct ?? 0,
                    Quantity = line.Quantity,
                    Price = p.Price
                });

                productUpdates[p.IdProduct ?? 0] = line.Quantity;
            }

            // Создаем заказ с правильной структурой
            var orderCreate = new Order
            {
                UserId = uid.Value,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                DeliveryDate = dto.DeliveryDate, // Используем выбранную дату
                DeliverySlotId = dto.DeliverySlotId,
                OrderStatusId = 1
            };

            // Остальной код создания заказа...
            var createResp = await _http.PostAsJsonAsync("Orders", orderCreate);
            if (!createResp.IsSuccessStatusCode)
            {
                var msg = await createResp.Content.ReadAsStringAsync();
                log.Error("Ошибка создания заказа. UserId={UserId}. Code={Code}, Msg={Msg}", uid, createResp.StatusCode, msg);
                TempData["OrderError"] = "Не удалось оформить заказ.";
                return RedirectToAction(nameof(Index));
            }

            var createdOrder = await createResp.Content.ReadFromJsonAsync<Order>();
            var orderId = createdOrder?.IdOrder ?? 0;

            if (orderId <= 0)
            {
                log.Error("API вернул некорректный Id заказа. UserId={UserId}", uid);
                TempData["OrderError"] = "Не удалось оформить заказ.";
                return RedirectToAction(nameof(Index));
            }

            // Добавляем позиции заказа
            foreach (var item in orderItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                var itemResp = await _http.PostAsJsonAsync("OrderDetails", orderDetail);
                if (!itemResp.IsSuccessStatusCode)
                {
                    var msg = await itemResp.Content.ReadAsStringAsync();
                    log.Warning("Ошибка добавления позиции в заказ {OrderId}. ProductId={ProductId}, Code={Code}",
                        orderId, item.ProductId, itemResp.StatusCode);
                }
            }

            // Обновляем количество товаров
            foreach (var update in productUpdates)
            {
                var product = products.FirstOrDefault(p => p.IdProduct == update.Key);
                if (product != null)
                {
                    var updatedProduct = new
                    {
                        IdProduct = product.IdProduct,
                        Name = product.Name,
                        Article = product.Article,
                        CategoryId = product.CategoryId,
                        ManufacturerId = product.ManufacturerId,
                        Unit = product.Unit,
                        VolumeOrWeight = product.VolumeOrWeight,
                        Description = product.Description,
                        Image = product.Image,
                        Price = product.Price,
                        Quantity = product.Quantity - update.Value,
                        IsAvailable = product.Quantity - update.Value > 0,
                        Deleted = product.Deleted,
                        CaloriesKcal = product.CaloriesKcal,
                        ProteinG = product.ProteinG,
                        FatG = product.FatG,
                        CarbsG = product.CarbsG
                    };

                    var updateResp = await _http.PutAsJsonAsync($"Products/{update.Key}", updatedProduct);
                    if (!updateResp.IsSuccessStatusCode)
                    {
                        log.Warning("Не удалось обновить количество товара {ProductId}. Code={Code}",
                            update.Key, updateResp.StatusCode);
                    }
                }
            }

            // Очищаем корзину
            foreach (var cartItem in userCart)
            {
                if (cartItem.IdCart.HasValue)
                {
                    await _http.DeleteAsync($"Carts/{cartItem.IdCart.Value}");
                }
            }

            log.Information("Заказ успешно оформлен. OrderId={OrderId}, UserId={UserId}, Total={Total}, DeliveryDate={DeliveryDate}",
                orderId, uid, total, dto.DeliveryDate);

            TempData["OrderId"] = orderId.ToString();
            TempData["OrderTotal"] = total.ToString("F2");

            return RedirectToAction(nameof(Success));
        }        // GET /Checkout/Success
        [HttpGet]
        public IActionResult Success()
        {
            var orderIdStr = TempData["OrderId"] as string;
            var totalStr = TempData["OrderTotal"] as string;

            // Парсим значения обратно
            if (!int.TryParse(orderIdStr, out var orderId))
            {
                orderId = 0;
            }

            if (!decimal.TryParse(totalStr, out var total))
            {
                total = 0m;
            }

            var vm = new OrderSuccessVm
            {
                OrderId = orderId,
                Total = total
            };

            return View(vm);
        }
    }

    public class CheckoutVm
    {
        public int UserId { get; set; }
        public List<CheckoutLine> Lines { get; set; } = new();
        public List<UserAddress> Addresses { get; set; } = new();
        public List<DeliveryTimeSlot> Slots { get; set; } = new();
        public decimal Total { get; set; }
        public DateOnly MinDeliveryDate { get; set; }
        public DateOnly DefaultDeliveryDate { get; set; }
    }

    public class CheckoutLine
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PlaceOrderDto
    {
        public int AddressId { get; set; }
        public int DeliverySlotId { get; set; }
        public DateOnly DeliveryDate { get; set; }
    }

    public class OrderDto
    {
        public int? IdOrder { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DeliveryDate { get; set; }
        public int DeliverySlotId { get; set; }
        public int? OrderStatusId { get; set; }
    }

    public class OrderSuccessVm
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderDetailDto
    {
        public int? IdOrderDetail { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
