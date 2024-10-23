namespace Comments_app.Common.Models
{
    public class Comment(string text, string captcha)
    {
        public int Id { get; set; }
        public string Text { get; set; } = text;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Captcha { get; set; } = captcha;
        public string? FilePath { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = [];
    }
}
