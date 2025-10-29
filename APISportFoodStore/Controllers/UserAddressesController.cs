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
    public class UserAddressesController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public UserAddressesController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/UserAddresses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAddress>>> GetUserAddresses()
        {
            return await _context.UserAddresses
                                 .Where(a => !a.Deleted)
                                 .ToListAsync();
        }

        // GET: api/UserAddresses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAddress>> GetUserAddress(int? id)
        {
            var userAddress = await _context.UserAddresses
                                            .Where(a => !a.Deleted && a.IdAddress == id)
                                            .FirstOrDefaultAsync();

            if (userAddress == null)
                return NotFound();

            return userAddress;
        }

        // PUT: api/UserAddresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserAddress(int? id, UserAddress userAddress)
        {
            if (id != userAddress.IdAddress)
                return BadRequest();

            _context.Entry(userAddress).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserAddressExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/UserAddresses
        [HttpPost]
        public async Task<ActionResult<UserAddress>> PostUserAddress(UserAddress userAddress)
        {
            userAddress.Deleted = false;
            _context.UserAddresses.Add(userAddress);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserAddress),
                new { id = userAddress.IdAddress }, userAddress);
        }

        // DELETE: api/UserAddresses/5 (логическое удаление)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAddress(int? id)
        {
            var userAddress = await _context.UserAddresses.FindAsync(id);
            if (userAddress == null || userAddress.Deleted)
                return NotFound();

            userAddress.Deleted = true;
            _context.Entry(userAddress).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserAddressExists(int? id)
        {
            return _context.UserAddresses.Any(e => e.IdAddress == id && !e.Deleted);
        }
    }
}
