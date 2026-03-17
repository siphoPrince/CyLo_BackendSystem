using Cylo_Backend.Data;
using Cylo_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cylo_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context) 
        { 
            _context = context;
        }


        [Authorize]
        [HttpPost("toggle/{postId}")]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null) return Unauthorized();

            if (!int.TryParse(userIdString, out int userId))
            {
                return BadRequest("Invalid User ID format");
            }

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            // We need the Post object to update the LikeCount
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found");

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                post.LikeCount--; 
            }
            else
            {
                var newLike = new Like
                {
                    PostId = postId,
                    UserId = userId
                };
                _context.Likes.Add(newLike);
                post.LikeCount++; 
    }

            await _context.SaveChangesAsync();

            
            return Ok(new { isLiked = existingLike == null, likeCount = post.LikeCount });
        }
    }
}
