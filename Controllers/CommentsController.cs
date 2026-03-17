using Cylo_Backend.Data;
using Cylo_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cylo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Comments>> PostComment(Comments comment)
        {
            // 1. Get the Identity from the token
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // 2. Verify the Post exists
            var postExists = await _context.Posts.AnyAsync(p => p.Id == comment.PostId);
            if (!postExists)
            {
                return NotFound("The post you are trying to comment on does not exist.");
            }

            // 3. Set the metadata
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;

            // 4. Finally, add and save
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Comments>> GetComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            return comment;
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<IEnumerable<Comments>>> GetCommentsByPost(int postId)
        {
            // We want to find all comments where the PostId matches the one from the URL
            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt) // Newest comments at the top
                .ToListAsync();

            return Ok(comments);
        }
    }
}
