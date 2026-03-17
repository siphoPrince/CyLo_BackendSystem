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
            int itemsToSkip = (pageNumber - 1) * pageSize;

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = int.TryParse(claimId, out int userId) ? userId : null;

            var posts = await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Category)
                .Skip(itemsToSkip)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<PostResponseDto>
            {
                Data = posts.Select(p => MapToDto(p, currentUserId)).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasNextPage = (pageNumber * pageSize) < totalCount,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        // 2. GET: api/Posts/{id} (For the BuyNow Page) 🎯
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = int.TryParse(claimId, out int userId) ? userId : null;

            var post = await _context.Posts
                .Include(p => p.User)
                    .ThenInclude(u => u.Profile)
                .Include(p => p.Category)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound(new { message = "Post not found." });

            return Ok(MapToDto(post, currentUserId));
        }

        [Authorize] 
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] Post post)
        {
            
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
                var newPost = await _context.Posts
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == post.Id);

                return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, MapToDto(newPost, currentUserId));
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Could not save the post. Check if CategoryId is valid.");
            }

        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] Post updatedPost)
        {
            
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

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPosts(int userId, int pageNumber = 1, int pageSize = 10) 
        {
            int totalCount = await _context.Posts.Where(p => p.UserId == userId).CountAsync();
            int itemsToSkip = (pageNumber - 1) * pageSize;
            bool hasNextPage = (pageNumber * pageSize) < totalCount;


            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = int.TryParse(claimId, out int id) ? id : null;


            // 2. Fetch only this user's posts with their Category and User info 📦
            var posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(itemsToSkip)
                .Take(pageSize)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,

                    Description = p.Description,
                    Price = p.Price,
                    MediaUrl = p.MediaUrl,
                    LikeCount = p.LikeCount,
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    IsLikedByCurrentUser = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value)
                })
                .ToListAsync();

            var response = new PagedResponse<PostResponseDto>
            {
                Data = posts,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasNextPage = hasNextPage,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        private PostResponseDto MapToDto(Post post, int? currentUserId)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Name = post.User?.Profile?.Name,
                HandleName = post.User?.Profile?.HandleName,
                SurName = post.User?.Profile?.SurName,
                Description = post.Description,
                Bio = post.User?.Profile?.Bio,
                ProfilePictureUrl = post.User?.Profile?.ImageUrl,
                Price = post.Price,
                MediaUrl = post.MediaUrl,
                LikeCount = post.LikeCount,
                CategoryName = post.Category != null ? post.Category.Name : "General",
                IsLikedByCurrentUser = currentUserId.HasValue &&
                                       post.Likes != null &&
                                       post.Likes.Any(l => l.UserId == currentUserId.Value)
            };
        }
    }
}
