using System.ComponentModel.DataAnnotations;

namespace CommentApp.Common.Models.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public string Text { get; set; }

        [Required]
        public string Captcha { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }

        public string? HomePage { get; set; }
        public IFormFile? Image { get; set; }

        public IFormFile? TextFile { get; set; }

        public int? ParentCommentId { get; set; }
    }

}
