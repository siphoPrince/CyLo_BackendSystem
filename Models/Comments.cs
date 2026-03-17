using System.ComponentModel.DataAnnotations;

namespace Cylo_Backend.Models
{
    public class Comments
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int PostId { get; set; }
        public string? UserId { get; set; }
    }
}
