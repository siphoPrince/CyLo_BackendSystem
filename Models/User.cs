using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cylo_Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; }
        [JsonIgnore]
        public Profile? Profile { get; set; }
        public string? ProfilePicture { get; internal set; }
    }
}
