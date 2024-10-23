using System.ComponentModel.DataAnnotations;

namespace Comments_app.Common.Models
{
    public class User(string userName, string email)
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = userName;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = email;
        public string? HomePage { get; set; }
        public ICollection<Comment> Comments { get; set; } = [];
    }
}
