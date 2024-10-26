using CommentApp.Common.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CommentApp.Common.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController(IProducer<Null, string> kafkaProducer, ILogger<CommentsController> logger) : ControllerBase
    {
        private readonly IProducer<Null, string> kafkaProducer = kafkaProducer;
        private readonly ILogger<CommentsController> logger = logger;

        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] Comment comment)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for Comment: {@Comment}", comment);
                return BadRequest(ModelState);
            }
            try
            {
                var commentJson = JsonConvert.SerializeObject(comment);
                await kafkaProducer.ProduceAsync("comments-new", new Message<Null, string> { Value = commentJson });
                //logger.LogInformation("Comment successfully sent to Kafka: {@Comment}", comment);
                return Ok(new { Message = "Comment successfully sent to Kafka." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending comment to Kafka.");
                return StatusCode(500, $"Error sending message to Kafka: {ex.Message}");
            }
        }
    }
}
