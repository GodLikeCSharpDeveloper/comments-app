using System.ComponentModel.DataAnnotations;

namespace CommentApp.Common.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string? HomePage { get; set; }
        public List<Comment> Comments { get; set; } = [];
    }
}
