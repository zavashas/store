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
    public class ReviewImagesController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public ReviewImagesController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/ReviewImages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewImage>>> GetReviewImages()
        {
            return await _context.ReviewImages.ToListAsync();
        }

        // GET: api/ReviewImages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReviewImage>> GetReviewImage(int? id)
        {
            var reviewImage = await _context.ReviewImages.FindAsync(id);

            if (reviewImage == null)
            {
                return NotFound();
            }

            return reviewImage;
        }

        // PUT: api/ReviewImages/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReviewImage(int? id, ReviewImage reviewImage)
        {
            if (id != reviewImage.IdReviewImage)
            {
                return BadRequest();
            }

            _context.Entry(reviewImage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewImageExists(id))
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

        // POST: api/ReviewImages
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ReviewImage>> PostReviewImage(ReviewImage reviewImage)
        {
            _context.ReviewImages.Add(reviewImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReviewImage", new { id = reviewImage.IdReviewImage }, reviewImage);
        }

        // DELETE: api/ReviewImages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReviewImage(int? id)
        {
            var reviewImage = await _context.ReviewImages.FindAsync(id);
            if (reviewImage == null)
            {
                return NotFound();
            }

            _context.ReviewImages.Remove(reviewImage);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReviewImageExists(int? id)
        {
            return _context.ReviewImages.Any(e => e.IdReviewImage == id);
        }
    }
}
