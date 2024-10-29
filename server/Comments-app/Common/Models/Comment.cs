namespace CommentApp.Common.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Captcha { get; set; }
        public string? ImageUrl { get; set; }
        public string? TextFileUrl { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } 
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = [];
    }
}
