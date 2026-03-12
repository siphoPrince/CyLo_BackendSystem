using Cylo_Backend.Data;
using Cylo_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cylo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly FileService _fileService;
        private readonly ApplicationDbContext _context;

        public UploadsController(FileService fileService, ApplicationDbContext context)
        {
            _fileService = fileService;
            _context = context;
        }

        [HttpPost("create-post")]
        public async Task<IActionResult> CreatePost([FromForm] string title,
                                            [FromForm] string description,
                                            [FromForm] decimal price,
                                            [FromForm] int categoryId,
                                            [FromForm] int userId,
                                            IFormFile file)
        {
            try
            {
                // 1. Save the file using your FileService 💾
                string fileName = await _fileService.SaveFileAsync(file);

                // 2. Create the new Post object 📦
                var newPost = new Post
                {
                    Title = title,
                    Description = description,
                    Price = price,
                    CategoryId = categoryId,
                    UserId = userId,
                    MediaUrl = fileName, // Storing the filename to find it later
                    CreatedAt = DateTime.UtcNow,
                    Status = "Available"
                };

                // 3. Save to SQL Database 🗄️
                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post created successfully!", postId = newPost.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}
