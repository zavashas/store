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
    public class ChatEventsController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public ChatEventsController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/ChatEvents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatEvent>>> GetChatEvents()
        {
            return await _context.ChatEvents.ToListAsync();
        }

        // GET: api/ChatEvents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChatEvent>> GetChatEvent(long? id)
        {
            var chatEvent = await _context.ChatEvents.FindAsync(id);

            if (chatEvent == null)
            {
                return NotFound();
            }

            return chatEvent;
        }

        // PUT: api/ChatEvents/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChatEvent(long? id, ChatEvent chatEvent)
        {
            if (id != chatEvent.IdEvent)
            {
                return BadRequest();
            }

            _context.Entry(chatEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChatEventExists(id))
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

        // POST: api/ChatEvents
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ChatEvent>> PostChatEvent(ChatEvent chatEvent)
        {
            _context.ChatEvents.Add(chatEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChatEvent", new { id = chatEvent.IdEvent }, chatEvent);
        }

        // DELETE: api/ChatEvents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatEvent(long? id)
        {
            var chatEvent = await _context.ChatEvents.FindAsync(id);
            if (chatEvent == null)
            {
                return NotFound();
            }

            _context.ChatEvents.Remove(chatEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChatEventExists(long? id)
        {
            return _context.ChatEvents.Any(e => e.IdEvent == id);
        }
    }
}
