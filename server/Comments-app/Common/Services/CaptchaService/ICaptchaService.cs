namespace CommentApp.Common.Services.CaptchaService
{
    public interface ICaptchaService
    {
        Task<bool> ValidateToken(string token);
    }
}
