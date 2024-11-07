using CommentApp.Common.Models;
using Newtonsoft.Json;


namespace CommentApp.Common.Services.CaptchaService
{
    public class CaptchaService(IConfiguration configuration) : ICaptchaService
    {
        private readonly string captchaSecretKey = configuration["ReCaptcha:SecretKey"];
        public async Task<bool> ValidateToken(string token)
        {
            var client = new HttpClient();
            var secret = captchaSecretKey;
            var response = await client.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}");
            var verificationResult = JsonConvert.DeserializeObject<CaptchaResponse>(response);
            if (verificationResult == null)
                return false;
            return verificationResult.Success;
        }
    }
}