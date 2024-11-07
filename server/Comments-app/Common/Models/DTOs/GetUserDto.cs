namespace CommentApp.Common.Models.DTOs
{
    public class GetUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? HomePage { get; set; }
    }
}
