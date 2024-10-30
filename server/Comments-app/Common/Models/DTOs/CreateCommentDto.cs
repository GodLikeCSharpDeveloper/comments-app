using System.ComponentModel.DataAnnotations;

namespace CommentApp.Common.Models.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        [StringLength(200)]
        public string Text { get; set; }

        [Required]
        [StringLength(20)]
        public string Captcha { get; set; }
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }
        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        public string? HomePage { get; set; }
        public IFormFile? Image { get; set; }

        public IFormFile? TextFile { get; set; }

        public int? ParentCommentId { get; set; }
    }

}
