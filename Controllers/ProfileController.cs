using Cylo_Backend.Data;
using Cylo_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cylo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protects all routes in this controller
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Profile/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<Profile>> GetProfile(int userId)
        {

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return NotFound();
            }

            // 📊 Calculate the counts from the Follows table
            var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == userId);
            return Ok( new { profile, followersCount, followingCount });
        }

        // POST: api/Profile/save
        // This handles BOTH creating for the first time and updating existing profiles
        [HttpPost("save")]
        public async Task<ActionResult<Profile>> SaveProfile([FromBody] Profile incomingProfile)
        {
            // 1. Get the logged-in User's ID from the JWT Token
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimId, out int currentUserId))
            {
                return Unauthorized();
            }

            // 2. Check if this user already has a profile record
            var existingProfile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (existingProfile == null)
            {
                // CASE: NEW USER (CREATE)
                // Force the UserId to match the token for security
                incomingProfile.UserId = currentUserId;
                incomingProfile.Id = 0; // Let Database generate the primary key

                _context.Profiles.Add(incomingProfile);
                await _context.SaveChangesAsync();

                return Ok(incomingProfile);
            }
            else
            {
                // CASE: EXISTING USER (UPDATE)
                // Map values from the request to the existing database record
                existingProfile.Name = incomingProfile.Name;
                existingProfile.SurName = incomingProfile.SurName;
                existingProfile.HandleName = incomingProfile.HandleName;
                existingProfile.Bio = incomingProfile.Bio;
                existingProfile.Phone = incomingProfile.Phone;
                existingProfile.ImageUrl = incomingProfile.ImageUrl;

                await _context.SaveChangesAsync();
                return Ok(existingProfile);
            }
        }
    }
}