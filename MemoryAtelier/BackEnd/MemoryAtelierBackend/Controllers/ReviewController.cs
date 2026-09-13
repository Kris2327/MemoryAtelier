using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReviewController(AppDbContext db) { _db = db; }

    // GET api/review/{productId}
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var reviews = await _db.Reviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                UserName = r.UserName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // POST api/review/{productId}
    [HttpPost("{productId}")]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Rating must be between 1 and 5.");

        if (string.IsNullOrWhiteSpace(dto.Comment))
            return BadRequest("Comment is required.");

        // вземи името от JWT ако си логнат, иначе "Anonymous"
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Anonymous";

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserName = userName,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return Ok(new ReviewDto
        {
            Id = review.Id,
            UserName = review.UserName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }

    // DELETE api/review/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null) return NotFound();
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}