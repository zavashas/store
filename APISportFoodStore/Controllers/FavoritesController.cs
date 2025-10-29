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
    public class FavoritesController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public FavoritesController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites()
        {
            return await _context.Favorites.ToListAsync();
        }

        // GET: api/Favorites/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Favorite>> GetFavorite(int? id)
        {
            var favorite = await _context.Favorites.FindAsync(id);

            if (favorite == null)
            {
                return NotFound();
            }

            return favorite;
        }

        // PUT: api/Favorites/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFavorite(int? id, Favorite favorite)
        {
            if (id != favorite.IdFavorite)
            {
                return BadRequest();
            }

            _context.Entry(favorite).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FavoriteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Favorites
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Favorite>> PostFavorite(Favorite favorite)
        {
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFavorite", new { id = favorite.IdFavorite }, favorite);
        }

        // DELETE: api/Favorites/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavorite(int? id)
        {
            var favorite = await _context.Favorites.FindAsync(id);
            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FavoriteExists(int? id)
        {
            return _context.Favorites.Any(e => e.IdFavorite == id);
        }

        // GET: api/Favorites/by-user/123 -
        [HttpGet("by-user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<int>>> GetByUser(int userId)
        {
            var ids = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();
            return Ok(ids);
        }

        // POST: api/Favorites/toggle
        public record ToggleFavoriteDto(int UserId, int ProductId);

        [HttpPost("toggle")]
        public async Task<ActionResult<object>> Toggle([FromBody] ToggleFavoriteDto dto)
        {
            if (dto.UserId <= 0 || dto.ProductId <= 0) return BadRequest("Bad payload");

            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == dto.UserId && f.ProductId == dto.ProductId);

            if (existing == null)
            {
                _context.Favorites.Add(new Favorite { UserId = dto.UserId, ProductId = dto.ProductId });
                await _context.SaveChangesAsync();
                return Ok(new { inFavorites = true });
            }
            else
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { inFavorites = false });
            }
        }

    }
}
