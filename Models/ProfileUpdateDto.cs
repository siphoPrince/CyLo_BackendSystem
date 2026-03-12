using System.ComponentModel.DataAnnotations;

namespace Cylo_Backend.Models
{
    public class ProfileUpdateDto
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? SurName { get; set; }
        [Required]
        public string? Phone { get; set; }
        public string? ImageUrl { get; set; }
    }
}
