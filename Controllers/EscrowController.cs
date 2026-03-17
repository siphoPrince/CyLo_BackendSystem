using Cylo_Backend.Data;
using Cylo_Backend.Models.Escrow_and_Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Cylo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EscrowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EscrowController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] EscrowOrder order)
        {
            // A. Extract verified ID from Token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(new { message = "User identity missing." });

            int currentUserId = int.Parse(userIdClaim.Value);

            // B. Fetch Data
            var post = await _context.Posts.FindAsync(order.PostId);
            if (post == null) return NotFound(new { message = "Listing not found." });

            var buyer = await _context.Users.FindAsync(currentUserId); // Use Token ID
            if (buyer == null) return BadRequest(new { message = "Buyer not found." });

            // C. Sync and Save Order
            order.BuyerId = currentUserId;
            order.Amount = post.Price; // Force correct price from DB
            order.Status = EscrowStatus.Pending;
            order.CreatedAt = DateTime.UtcNow;

            _context.EscrowOrders.Add(order);
            await _context.SaveChangesAsync();

            // D. Paystack Handshake
            string apiKey = _configuration["Paystack:SecretKey"];
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var paystackPayload = new
            {
                email = buyer.Email,
                amount = (int)(post.Price * 100),
                reference = order.Id.ToString(),
                callback_url = "http://localhost:3000/verify-order"
            };

            var response = await client.PostAsJsonAsync($"{_configuration["Paystack:BaseUrl"]}transaction/initialize", paystackPayload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PaystackInitializeResponse>();
                return Ok(new { checkoutUrl = result?.Data?.AuthorizationUrl });
            }

            return StatusCode(500, "Payment initialization failed.");
        }
    }

    // Helper classes for parsing Paystack's JSON response
    public class PaystackInitializeResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public PaystackData Data { get; set; }
    }

    public class PaystackData
    {
        [System.Text.Json.Serialization.JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; set; }
        public string AccessCode { get; set; }
        public string Reference { get; set; }
    }
}