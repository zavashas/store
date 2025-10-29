using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APISportFoodStore.Models;

namespace APISportFoodStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturersController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public ManufacturersController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Manufacturers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Manufacturer>>> GetManufacturers()
        {
            return await _context.Manufacturers
                                 .Where(m => !m.Deleted)
                                 .ToListAsync();
        }

        // GET: api/Manufacturers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Manufacturer>> GetManufacturer(int? id)
        {
            var manufacturer = await _context.Manufacturers
                                             .Where(m => !m.Deleted && m.IdManufacturer == id)
                                             .FirstOrDefaultAsync();

            if (manufacturer == null) return NotFound();
            return manufacturer;
        }

        // PUT: api/Manufacturers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutManufacturer(int? id, Manufacturer dto)
        {
            if (id != dto.IdManufacturer) return BadRequest();

            var entity = await _context.Manufacturers.FirstOrDefaultAsync(m => m.IdManufacturer == id);
            if (entity == null) return NotFound();

            // нормализуем имя
            var normalizedName = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                return BadRequest("Название производителя обязательно.");

            // если после обновления производитель активен — проверяем уникальность имени среди активных
            if (!dto.Deleted)
            {
                var exists = await _context.Manufacturers
                    .AnyAsync(m => !m.Deleted
                                   && m.IdManufacturer != id
                                   && m.Name.ToLower() == normalizedName.ToLower());
                if (exists)
                    return Conflict("Производитель с таким названием уже существует.");
            }

            // переносим поля (избегаем оверпостинга)
            entity.Name = normalizedName;
            entity.Deleted = dto.Deleted;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ManufacturerExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // POST: api/Manufacturers
        [HttpPost]
        public async Task<ActionResult<Manufacturer>> PostManufacturer(Manufacturer manufacturer)
        {
            // нормализуем имя
            manufacturer.Name = manufacturer.Name?.Trim();
            if (string.IsNullOrWhiteSpace(manufacturer.Name))
                return BadRequest("Название производителя обязательно.");

            // активный производитель не должен дублировать имя другого активного
            var exists = await _context.Manufacturers
                .AnyAsync(m => !m.Deleted && m.Name.ToLower() == manufacturer.Name.ToLower());
            if (exists)
                return Conflict("Производитель с таким названием уже существует.");

            manufacturer.Deleted = false;

            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetManufacturer),
                new { id = manufacturer.IdManufacturer }, manufacturer);
        }

        // DELETE: api/Manufacturers/5 (логическое удаление)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManufacturer(int? id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null || manufacturer.Deleted) return NotFound();

            manufacturer.Deleted = true;
            _context.Entry(manufacturer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ManufacturerExists(int? id)
        {
            return _context.Manufacturers.Any(e => e.IdManufacturer == id && !e.Deleted);
        }
    }
}
