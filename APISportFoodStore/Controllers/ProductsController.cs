using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APISportFoodStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public ProductsController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        private static readonly string[] StaticUnits = new[]
        {
            "шт", "г", "мл"
        };


        // GET: api/products/units — статический список единиц для выпадающего списка
        [HttpGet("units")]
        public ActionResult<IEnumerable<string>> GetUnits()
        {
            return Ok(StaticUnits);
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Where(p => !p.Deleted)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.IdProduct == id && !p.Deleted);

            return product is null ? NotFound() : Ok(product);
        }

        // GET: api/products/with-details
        // Детальная выдача с именами категории и производителя (Unit — строка из продукта)
        [HttpGet("with-details")]
        public async Task<ActionResult> GetProductsWithDetails()
        {
            var products = await _context.Products
                .Where(p => !p.Deleted)
                .Join(_context.Categories,
                      p => p.CategoryId,
                      c => c.IdCategory,
                      (p, c) => new { Product = p, CategoryName = c.Name })
                .Join(_context.Manufacturers,
                      pc => pc.Product.ManufacturerId,
                      m => m.IdManufacturer,
                      (pc, m) => new
                      {
                          pc.Product.IdProduct,
                          pc.Product.Name,
                          pc.Product.Article,
                          pc.Product.VolumeOrWeight,
                          pc.Product.Unit,
                          pc.Product.Description,
                          pc.Product.Image,
                          pc.Product.Price,
                          pc.Product.Quantity,
                          pc.Product.IsAvailable,
                          pc.Product.Deleted,
                          Category = pc.CategoryName,
                          Manufacturer = m.Name,

                          // КБЖУ
                          pc.Product.CaloriesKcal,
                          pc.Product.ProteinG,
                          pc.Product.FatG,
                          pc.Product.CarbsG
                      })
                .ToListAsync();

            return Ok(products);
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (product == null)
                return BadRequest("Product data is null");

            // валидация справочников
            var category = await _context.Categories.FindAsync(product.CategoryId);
            var manufacturer = await _context.Manufacturers.FindAsync(product.ManufacturerId);
            if (category == null || category.Deleted || manufacturer == null || manufacturer.Deleted)
                return BadRequest("Invalid category or manufacturer.");

            // если артикул пуст — сгенерируем
            if (string.IsNullOrWhiteSpace(product.Article))
                product.Article = await GenerateUniqueArticle();

            product.Deleted = false;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.IdProduct }, product);
        }

        private async Task<string> GenerateUniqueArticle()
        {
            var random = new Random();

            while (true)
            {
                string article = DateTime.UtcNow.ToString("yyMMdd") + random.Next(0, 999999).ToString("D6");

                bool exists = await _context.Products.AnyAsync(p => p.Article == article);
                if (!exists)
                    return article;
            }
        }

        // PUT: api/products/5
        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product dto)
        {
            if (id != dto.IdProduct)
                return BadRequest();

            // 1) читаем текущий продукт (и проверяем, что он есть)
            var existing = await _context.Products
                .FirstOrDefaultAsync(p => p.IdProduct == id && !p.Deleted);
            if (existing == null)
                return NotFound();

            // 2) валидация справочников (если менялись ссылки)
            var categoryOk = await _context.Categories.AnyAsync(c => c.IdCategory == dto.CategoryId && !c.Deleted);
            var manufacturerOk = await _context.Manufacturers.AnyAsync(m => m.IdManufacturer == dto.ManufacturerId && !m.Deleted);
            if (!categoryOk) return BadRequest("Invalid category.");
            if (!manufacturerOk) return BadRequest("Invalid manufacturer.");

            existing.Name = dto.Name;
            existing.CategoryId = dto.CategoryId;
            existing.ManufacturerId = dto.ManufacturerId;
            existing.Unit = dto.Unit;
            existing.VolumeOrWeight = dto.VolumeOrWeight;
            existing.Description = dto.Description;
            existing.Image = dto.Image;         
            existing.Price = dto.Price;
            existing.Quantity = dto.Quantity;
            existing.IsAvailable = dto.IsAvailable;
            existing.Deleted = dto.Deleted;

            // КБЖУ
            existing.CaloriesKcal = dto.CaloriesKcal;
            existing.ProteinG = dto.ProteinG;
            existing.FatG = dto.FatG;
            existing.CarbsG = dto.CarbsG;

            // 4) сохраняем
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }


        // DELETE: api/products/5 — логическое удаление
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Deleted = true;
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.IdProduct == id && !e.Deleted);
        }


        // GET: api/products/public
        // Фильтры: search, sort(price_asc|price_desc), category
        [HttpGet("public")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetPublicProducts(
            [FromQuery] string? search,
            [FromQuery] string? sort,
            [FromQuery] int? category)
        {
            var query = _context.Products
                .Where(p => !p.Deleted && p.IsAvailable && p.Quantity > 0)
                .Join(_context.Manufacturers,
                      p => p.ManufacturerId,
                      m => m.IdManufacturer,
                      (p, m) => new { Product = p, Manufacturer = m });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(pm =>
                    pm.Product.Name.ToLower().Contains(s) ||
                    pm.Product.Description.ToLower().Contains(s));
            }

            if (category.HasValue)
            {
                query = query.Where(pm => pm.Product.CategoryId == category.Value);
            }

            if (sort == "price_asc")
                query = query.OrderBy(pm => pm.Product.Price);
            else if (sort == "price_desc")
                query = query.OrderByDescending(pm => pm.Product.Price);

            var products = await query
                .Select(pm => new ProductDto
                {
                    IdProduct = pm.Product.IdProduct!.Value,
                    Name = pm.Product.Name,
                    Article = pm.Product.Article,
                    Description = pm.Product.Description,
                    Image = pm.Product.Image,
                    Price = pm.Product.Price,
                    VolumeOrWeight = pm.Product.VolumeOrWeight,
                    Unit = pm.Product.Unit,
                    ManufacturerID = pm.Manufacturer.IdManufacturer!.Value,
                    ManufacturerName = pm.Manufacturer.Name,
                    CategoryId = pm.Product.CategoryId,

                    CaloriesKcal = pm.Product.CaloriesKcal,
                    ProteinG = pm.Product.ProteinG,
                    FatG = pm.Product.FatG,
                    CarbsG = pm.Product.CarbsG
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/products/public/{id}
        [HttpGet("public/{id}")]
        public async Task<ActionResult<ProductDto>> GetPublicProduct(int id)
        {
            var product = await _context.Products
                .Where(p => !p.Deleted && p.IsAvailable && p.Quantity > 0 && p.IdProduct == id)
                .Join(_context.Manufacturers,
                      p => p.ManufacturerId,
                      m => m.IdManufacturer,
                      (p, m) => new { Product = p, Manufacturer = m })
                .Select(pm => new ProductDto
                {
                    IdProduct = pm.Product.IdProduct!.Value,
                    Name = pm.Product.Name,
                    Article = pm.Product.Article,
                    Description = pm.Product.Description,
                    Image = pm.Product.Image,
                    Price = pm.Product.Price,
                    VolumeOrWeight = pm.Product.VolumeOrWeight,
                    Unit = pm.Product.Unit,
                    ManufacturerID = pm.Manufacturer.IdManufacturer!.Value,
                    ManufacturerName = pm.Manufacturer.Name,
                    CategoryId = pm.Product.CategoryId,

                    CaloriesKcal = pm.Product.CaloriesKcal,
                    ProteinG = pm.Product.ProteinG,
                    FatG = pm.Product.FatG,
                    CarbsG = pm.Product.CarbsG
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return Ok(product);
        }
    }
}
