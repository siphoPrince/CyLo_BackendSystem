namespace Cylo_Backend.Models
{
    public class PostResponseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? SurName { get; set; }
        public string? HandleName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Bio { get; set; }
        public decimal Price { get; set; }
        public string? MediaUrl { get; set; }
        public int LikeCount { get; set; }
        public string? CategoryName { get; set; }
        public string? Username { get; set; }        
        public string? ProfilePictureUrl { get; set; }

        public bool IsLikedByCurrentUser { get; set; }
    }
}
