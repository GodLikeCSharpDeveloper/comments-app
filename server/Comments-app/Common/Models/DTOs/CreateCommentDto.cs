
using CommentApp.Common.Validation;
using System.ComponentModel.DataAnnotations;

namespace CommentApp.Common.Models.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        [StringLength(200)]
        public string Text { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }
        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        public string? HomePage { get; set; }
        [MaxFileSize(1 * 1024 * 1024)]
        public IFormFile? Image { get; set; }
        [MaxFileSize(100 * 1024)]
        public IFormFile? TextFile { get; set; }

        public string? ParentCommentId { get; set; }
    }

}
