using Cylo_Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
   

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string? Title { get; set; }

    [Required]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public string Status { get; set; } = "Available";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    public string? MediaUrl { get; set; }
    public int LikeCount { get; set; } = 0;

    [JsonIgnore]
    public virtual User? User { get; set; }

    [JsonIgnore]
    public virtual Category? Category { get; set; }
    [JsonIgnore]
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}