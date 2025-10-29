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
    public class CategoriesController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public CategoriesController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                                 .Where(c => !c.Deleted)
                                 .ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int? id)
        {
            var category = await _context.Categories
                                         .Where(c => !c.Deleted && c.IdCategory == id)
                                         .FirstOrDefaultAsync();

            if (category == null)
                return NotFound();

            return category;
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int? id, Category dto)
        {
            if (id != dto.IdCategory)
                return BadRequest();

            var entity = await _context.Categories.FirstOrDefaultAsync(c => c.IdCategory == id);
            if (entity == null)
                return NotFound();

            var normalizedName = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                return BadRequest("Название категории обязательно.");

            if (!dto.Deleted)
            {
                var exists = await _context.Categories
                    .AnyAsync(c => !c.Deleted
                                   && c.IdCategory != id
                                   && c.Name.ToLower() == normalizedName.ToLower());
                if (exists)
                    return Conflict("Категория с таким названием уже существует.");
            }

            entity.Name = normalizedName;
            entity.Deleted = dto.Deleted;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            category.Name = category.Name?.Trim();
            if (string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Название категории обязательно.");

            // активная категория не должна дублировать имя другой активной
            var exists = await _context.Categories
                .AnyAsync(c => !c.Deleted && c.Name.ToLower() == category.Name.ToLower());
            if (exists)
                return Conflict("Категория с таким названием уже существует.");

            category.Deleted = false;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.IdCategory }, category);
        }

        // DELETE: api/Categories/5 (логическое удаление)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.Deleted)
                return NotFound();

            category.Deleted = true;
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int? id)
        {
            return _context.Categories.Any(e => e.IdCategory == id && !e.Deleted);
        }
    }
}
