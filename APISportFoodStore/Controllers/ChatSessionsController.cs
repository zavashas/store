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
    public class ChatSessionsController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public ChatSessionsController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/ChatSessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetChatSessions()
        {
            return await _context.ChatSessions.ToListAsync();
        }

        // GET: api/ChatSessions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChatSession>> GetChatSession(int? id)
        {
            var chatSession = await _context.ChatSessions.FindAsync(id);

            if (chatSession == null)
            {
                return NotFound();
            }

            return chatSession;
        }

        // PUT: api/ChatSessions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChatSession(int? id, ChatSession chatSession)
        {
            if (id != chatSession.IdChat)
            {
                return BadRequest();
            }

            _context.Entry(chatSession).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChatSessionExists(id))
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

        // POST: api/ChatSessions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ChatSession>> PostChatSession(ChatSession chatSession)
        {
            _context.ChatSessions.Add(chatSession);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChatSession", new { id = chatSession.IdChat }, chatSession);
        }

        // DELETE: api/ChatSessions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatSession(int? id)
        {
            var chatSession = await _context.ChatSessions.FindAsync(id);
            if (chatSession == null)
            {
                return NotFound();
            }

            _context.ChatSessions.Remove(chatSession);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChatSessionExists(int? id)
        {
            return _context.ChatSessions.Any(e => e.IdChat == id);
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartChat([FromBody] int customerUserId)
        {
            if (customerUserId <= 0)
                return BadRequest("customerUserId должен быть > 0");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == customerUserId);
            if (user == null) return NotFound($"Пользователь {customerUserId} не найден");

            var existing = await _context.ChatSessions
                .Where(c => c.CustomerUserId == customerUserId && c.Status == "open")
                .OrderByDescending(c => c.LastMessageAt)
                .FirstOrDefaultAsync();
            if (existing != null)
                return Ok(existing);

            var chat = new ChatSession
            {
                CustomerUserId = user.IdUser,
                StartedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
                Status = "open",
                Priority = 0,
                Deleted = false
            };

            _context.ChatSessions.Add(chat);
            await _context.SaveChangesAsync();
            return Ok(chat);
        }


        [HttpGet("by-customer/{customerId:int}/open")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetCustomerOpen(int customerId)
        {
            return await _context.ChatSessions
                .Where(c => c.CustomerUserId == customerId && c.Status == "open")
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        [HttpGet("by-customer/{customerId:int}/closed")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetCustomerClosed(int customerId)
        {
            return await _context.ChatSessions
                .Where(c => c.CustomerUserId == customerId && c.Status == "closed")
                .OrderByDescending(c => c.ClosedAt)
                .ToListAsync();
        }


        [HttpGet("queue")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetQueue()
        {
            return await _context.ChatSessions
                .Where(c => c.Status == "open" && c.AssignedAgentId == null)
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        [HttpGet("active/{agentId}")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetActiveChats(int agentId)
        {
            return await _context.ChatSessions
                .Where(c => c.Status == "open" && c.AssignedAgentId == agentId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        [HttpPost("{id}/claim")]
        public async Task<IActionResult> ClaimChat(int id, [FromBody] int agentId)
        {
            var chat = await _context.ChatSessions.FindAsync(id);
            if (chat == null || chat.AssignedAgentId != null) return NotFound();

            chat.AssignedAgentId = agentId;
            chat.LastMessageAt = DateTime.UtcNow;

            var presence = await _context.AgentPresences.FindAsync(agentId);
            if (presence != null)
            {
                presence.CurrentActive += 1;
                presence.UpdatedAt = DateTime.UtcNow;
            }

            _context.ChatEvents.Add(new ChatEvent
            {
                ChatId = id,
                ActorUserId = agentId,
                EventType = "claimed",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseChat(int id, [FromBody] int actorId)
        {
            var chat = await _context.ChatSessions.FindAsync(id);
            if (chat == null) return NotFound();

            chat.Status = "closed";
            chat.ClosedAt = DateTime.UtcNow;

            if (chat.AssignedAgentId.HasValue)
            {
                var presence = await _context.AgentPresences.FindAsync(chat.AssignedAgentId.Value);
                if (presence != null && presence.CurrentActive > 0)
                {
                    presence.CurrentActive -= 1;
                    presence.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.ChatEvents.Add(new ChatEvent
            {
                ChatId = id,
                ActorUserId = actorId,
                EventType = "closed",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // все открытые чаты
        [HttpGet("open")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetAllOpen()
        {
            return await _context.ChatSessions
                .Where(c => c.Status == "open")
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        [HttpPost("{id:int}/resolve")]
        public async Task<IActionResult> ResolveByCustomer(int id, [FromBody] int customerId)
        {
            var chat = await _context.ChatSessions.FindAsync(id);
            if (chat == null) return NotFound();
            if (chat.CustomerUserId != customerId) return Forbid();

            if (chat.Status == "closed") return Ok(); 

            chat.Status = "closed";
            chat.ClosedAt = DateTime.UtcNow;

            _context.ChatEvents.Add(new ChatEvent
            {
                ChatId = id,
                ActorUserId = customerId,
                EventType = "closed-by-customer",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // все закрытые (для Admin)
        [HttpGet("closed")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetClosedAll()
        {
            return await _context.ChatSessions
                .Where(c => c.Status == "closed")
                .OrderByDescending(c => c.ClosedAt.HasValue ? c.ClosedAt.Value : c.LastMessageAt)
                .ToListAsync();
        }

        // закрытые (для Manager)
        [HttpGet("closed-by-agent/{agentId:int}")]
        public async Task<ActionResult<IEnumerable<ChatSession>>> GetClosedByAgent(int agentId)
        {
            return await _context.ChatSessions
                .Where(c => c.Status == "closed" && c.AssignedAgentId == agentId)
                .OrderByDescending(c => c.ClosedAt.HasValue ? c.ClosedAt.Value : c.LastMessageAt)
                .ToListAsync();
        }
    }
}
