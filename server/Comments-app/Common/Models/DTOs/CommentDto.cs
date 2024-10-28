namespace CommentApp.Common.Models.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Captcha { get; set; }
        public int UserId { get; set; }
        public int? ParentCommentId { get; set; }
    }
}
