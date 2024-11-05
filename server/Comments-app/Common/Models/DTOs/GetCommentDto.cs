using System.ComponentModel.DataAnnotations;

namespace CommentApp.Common.Models.DTOs
{
    public class GetCommentDto
    {
        public string Text { get; set; }

        public string Captcha { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? HomePage { get; set; }
        public IFormFile? Image { get; set; }
        public IFormFile? TextFile { get; set; }
        public int? ParentCommentId { get; set; }
    }
}
