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
    public class DeliveryTimeSlotsController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public DeliveryTimeSlotsController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/DeliveryTimeSlots
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeliveryTimeSlot>>> GetDeliveryTimeSlots()
        {
            return await _context.DeliveryTimeSlots.ToListAsync();
        }

        // GET: api/DeliveryTimeSlots/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryTimeSlot>> GetDeliveryTimeSlot(int? id)
        {
            var deliveryTimeSlot = await _context.DeliveryTimeSlots.FindAsync(id);

            if (deliveryTimeSlot == null)
            {
                return NotFound();
            }

            return deliveryTimeSlot;
        }

        // PUT: api/DeliveryTimeSlots/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeliveryTimeSlot(int? id, DeliveryTimeSlot deliveryTimeSlot)
        {
            if (id != deliveryTimeSlot.IdDeliverySlot)
            {
                return BadRequest();
            }

            _context.Entry(deliveryTimeSlot).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeliveryTimeSlotExists(id))
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

        // POST: api/DeliveryTimeSlots
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DeliveryTimeSlot>> PostDeliveryTimeSlot(DeliveryTimeSlot deliveryTimeSlot)
        {
            _context.DeliveryTimeSlots.Add(deliveryTimeSlot);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDeliveryTimeSlot", new { id = deliveryTimeSlot.IdDeliverySlot }, deliveryTimeSlot);
        }

        // DELETE: api/DeliveryTimeSlots/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliveryTimeSlot(int? id)
        {
            var deliveryTimeSlot = await _context.DeliveryTimeSlots.FindAsync(id);
            if (deliveryTimeSlot == null)
            {
                return NotFound();
            }

            _context.DeliveryTimeSlots.Remove(deliveryTimeSlot);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeliveryTimeSlotExists(int? id)
        {
            return _context.DeliveryTimeSlots.Any(e => e.IdDeliverySlot == id);
        }
    }
}
