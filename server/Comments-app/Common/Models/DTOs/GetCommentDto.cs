namespace CommentApp.Common.Models.DTOs
{
    public class GetCommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public GetUserDto User { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? TextFileUrl { get; set; }
        public List<GetCommentDto>? Replies { get; set; }
    }
}
