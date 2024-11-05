using Newtonsoft.Json;

namespace CommentApp.Common.Models
{
    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
