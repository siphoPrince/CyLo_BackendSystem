using Cylo_Backend.Data;
using Cylo_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cylo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts(int pageNumber = 1, int pageSize = 10)
        {
            int totalCount = await _context.Posts.CountAsync();
            // Step 1: Calculate how many items to skip
            int itemsToSkip = (pageNumber - 1) * pageSize;
            bool hasNextPage = (pageNumber * pageSize) < totalCount;

            // Step 2: Fetch the data from the database
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(itemsToSkip)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<Post>
            {
                Data = posts,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasNextPage = hasNextPage,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        [Authorize] // 🔒 Requires a valid JWT
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] Post post)
        {
            // 1. Extract User ID from the JWT token claims
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimId, out int currentUserId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // 2. Force Security & Integrity
            post.UserId = currentUserId; // 🛡️ Overwrites any spoofed ID from the frontend
            post.Id = 0;                 // Let DB handle the Primary Key
            post.CreatedAt = DateTime.UtcNow;

            _context.Posts.Add(post);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Could not save the post. Check if CategoryId is valid.");
            }

            // Return 201 Created and include the new object
            return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, post);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] Post updatedPost)
        {
            // 1. Get current User ID from token
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimId, out int currentUserId)) return Unauthorized();

            // 2. Find the existing post
            var existingPost = await _context.Posts.FindAsync(id);

            if (existingPost == null) return NotFound();

            // 3. Security: Does the user own this post? 🛡️
            if (existingPost.UserId != currentUserId)
            {
                return Forbid(); // 403 Forbidden
            }

            // 4. Map the changes
            existingPost.Title = updatedPost.Title;
            existingPost.Description = updatedPost.Description;
            existingPost.Price = updatedPost.Price;
            existingPost.CategoryId = updatedPost.CategoryId;
            // Note: We don't touch existingPost.CreatedAt or existingPost.UserId!

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Database error during update.");
            }

            return NoContent(); // 204 No Content is standard for a successful PUT
        }
    }
}
