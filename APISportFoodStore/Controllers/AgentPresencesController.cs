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
    public class AgentPresencesController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public AgentPresencesController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/AgentPresences
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgentPresence>>> GetAgentPresences()
        {
            return await _context.AgentPresences.ToListAsync();
        }

        // GET: api/AgentPresences/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AgentPresence>> GetAgentPresence(int? id)
        {
            var agentPresence = await _context.AgentPresences.FindAsync(id);

            if (agentPresence == null)
            {
                return NotFound();
            }

            return agentPresence;
        }

        // PUT: api/AgentPresences/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgentPresence(int? id, AgentPresence agentPresence)
        {
            if (id != agentPresence.AgentUserId)
            {
                return BadRequest();
            }

            _context.Entry(agentPresence).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgentPresenceExists(id))
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

        // POST: api/AgentPresences
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AgentPresence>> PostAgentPresence(AgentPresence agentPresence)
        {
            _context.AgentPresences.Add(agentPresence);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (AgentPresenceExists(agentPresence.AgentUserId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetAgentPresence", new { id = agentPresence.AgentUserId }, agentPresence);
        }

        // DELETE: api/AgentPresences/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgentPresence(int? id)
        {
            var agentPresence = await _context.AgentPresences.FindAsync(id);
            if (agentPresence == null)
            {
                return NotFound();
            }

            _context.AgentPresences.Remove(agentPresence);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AgentPresenceExists(int? id)
        {
            return _context.AgentPresences.Any(e => e.AgentUserId == id);
        }
    }
}
