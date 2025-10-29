using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APISportFoodStore.Models;


namespace APISportFoodStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;
        private readonly IEmailSender _email;

        public OrdersController(SportFoodStoreDbContext context, IEmailSender email)
        {
            _context = context;
            _email = email;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int? id)
        {
            if (!id.HasValue)
                return BadRequest("ID заказа не указан");

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound($"Заказ с ID {id} не найден");

            return order;
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int? id, Order order)
        {
            try
            {
                if (!id.HasValue)
                    return BadRequest("ID заказа не указан");

                if (id != order.IdOrder)
                    return BadRequest($"ID в URL ({id}) не совпадает с ID в теле запроса ({order.IdOrder})");

                var existingOrder = await _context.Orders.FindAsync(id);
                if (existingOrder == null)
                    return NotFound($"Заказ с ID {id} не найден");

                existingOrder.OrderStatusId = order.OrderStatusId;
                existingOrder.DeliveryDate = order.DeliveryDate;
                existingOrder.DeliverySlotId = order.DeliverySlotId;

                if (existingOrder.DeliveryDate < DateOnly.FromDateTime(DateTime.Now))
                    return BadRequest("Дата доставки не может быть в прошлом");

                if (!existingOrder.OrderStatusId.HasValue || existingOrder.OrderStatusId <= 0)
                    return BadRequest("Неверный статус заказа");

                try
                {
                    await _context.SaveChangesAsync();
                    return NoContent();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(id))
                        return NotFound();
                    else
                        return StatusCode(500, "Ошибка параллельного доступа к данным");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Ошибка сохранения в базу данных: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order, CancellationToken ct)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.IdUser == order.UserId)
                .Select(u => new { u.Name, u.Email })
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                var items = await _context.OrderDetails.AsNoTracking()
                    .Where(od => od.OrderId == order.IdOrder)
                    .Join(_context.Products.AsNoTracking(),
                          od => od.ProductId,
                          p => p.IdProduct,
                         (od, p) => new { p.Name, od.Quantity, od.Price })
                    .ToListAsync(ct);

                var tupleItems = items.Select(i => (i.Name, i.Quantity, i.Price)).ToList();

                var safeOrder = new Order
                {
                    IdOrder = order.IdOrder,
                    DeliveryDate = order.DeliveryDate,
                    TotalAmount = order.TotalAmount
                };

                await _email.SendOrderConfirmationAsync(
                    toEmail: user.Email!,
                    fullName: user.Name ?? "Клиент",
                    order: safeOrder,
                    items: tupleItems
                );
            }

            return CreatedAtAction(nameof(GetOrder), new { id = order.IdOrder }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int? id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int? id)
        {
            return _context.Orders.Any(e => e.IdOrder == id);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderWithDetailsDto>>> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = new List<OrderWithDetailsDto>();

            foreach (var order in orders)
            {
                var orderDetails = await _context.OrderDetails
                    .Where(od => od.OrderId == order.IdOrder)
                    .Join(_context.Products,
                          od => od.ProductId,
                          p => p.IdProduct,
                          (od, p) => new OrderDetailWithProductDto
                          {
                              IdOrderDetail = od.IdOrderDetail,
                              OrderId = od.OrderId,
                              ProductId = od.ProductId,
                              ProductName = p.Name,
                              ProductImage = p.Image,
                              ProductArticle = p.Article,
                              Price = od.Price,
                              Quantity = od.Quantity,
                              Subtotal = od.Price * od.Quantity
                          })
                    .ToListAsync();

                var orderStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.IdOrderStatus == order.OrderStatusId);

                result.Add(new OrderWithDetailsDto
                {
                    Order = order,
                    OrderDetails = orderDetails,
                    StatusName = orderStatus?.Name ?? "Неизвестно",
                    TotalAmount = order.TotalAmount
                });
            }

            return Ok(result);
        }

        [HttpGet("user/{userId}/details/{orderId}")]
        public async Task<ActionResult<OrderWithDetailsDto>> GetUserOrderDetails(int userId, int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.IdOrder == orderId && o.UserId == userId);

            if (order == null)
                return NotFound();

            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Join(_context.Products,
                      od => od.ProductId,
                      p => p.IdProduct,
                      (od, p) => new OrderDetailWithProductDto
                      {
                          IdOrderDetail = od.IdOrderDetail,
                          OrderId = od.OrderId,
                          ProductId = od.ProductId,
                          ProductName = p.Name,
                          ProductImage = p.Image,
                          ProductArticle = p.Article,
                          Price = od.Price,
                          Quantity = od.Quantity,
                          Subtotal = od.Price * od.Quantity
                      })
                .ToListAsync();

            var orderStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.IdOrderStatus == order.OrderStatusId);

            var result = new OrderWithDetailsDto
            {
                Order = order,
                OrderDetails = orderDetails,
                StatusName = orderStatus?.Name ?? "Неизвестно",
                TotalAmount = order.TotalAmount
            };

            return Ok(result);
        }
    }

    public class OrderWithDetailsDto
    {
        public Order Order { get; set; }
        public List<OrderDetailWithProductDto> OrderDetails { get; set; }
        public string StatusName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderDetailWithProductDto
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
    }
}
