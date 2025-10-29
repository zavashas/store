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
    public class UserCardsController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public UserCardsController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/UserCards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserCard>>> GetUserCards()
        {
            return await _context.UserCards
                                 .Where(c => !c.Deleted)
                                 .ToListAsync();
        }

        // GET: api/UserCards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserCard>> GetUserCard(int? id)
        {
            var userCard = await _context.UserCards
                                         .Where(c => !c.Deleted && c.IdUserCard == id)
                                         .FirstOrDefaultAsync();

            if (userCard == null)
            {
                return NotFound();
            }

            return userCard;
        }

        // PUT: api/UserCards/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserCard(int? id, UserCard userCard)
        {
            if (id != userCard.IdUserCard)
            {
                return BadRequest();
            }

            _context.Entry(userCard).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserCardExists(id))
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

        // POST: api/UserCards
        [HttpPost]
        public async Task<ActionResult<UserCard>> PostUserCard(UserCard userCard)
        {
            userCard.Deleted = false;
            _context.UserCards.Add(userCard);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserCard),
                new { id = userCard.IdUserCard }, userCard);
        }

        // DELETE: api/UserCards/5 (логическое удаление)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserCard(int? id)
        {
            var userCard = await _context.UserCards.FindAsync(id);
            if (userCard == null || userCard.Deleted)
            {
                return NotFound();
            }

            userCard.Deleted = true;
            _context.Entry(userCard).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserCardExists(int? id)
        {
            return _context.UserCards.Any(e => e.IdUserCard == id && !e.Deleted);
        }
    }
}
