using AutoMapper;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Services.FileService;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CommentApp.Common.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class CommentsController(IProducer<Null, string> kafkaProducer, ILogger<CommentsController> logger, IMapper mapper, IFileService fileService, KafkaQueueService kafkaQueueService) : ControllerBase
    {
        private readonly IProducer<Null, string> kafkaProducer = kafkaProducer;
        private readonly ILogger<CommentsController> logger = logger;
        private readonly IMapper mapper = mapper;
        private readonly IFileService fileService = fileService;
        private readonly KafkaQueueService kafkaQueueService = kafkaQueueService;

        [HttpPost]
        public async Task<IActionResult> PostComment([FromForm] CreateCommentDto request)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for Comment: {@Comment}", request);
                return BadRequest(ModelState);
            }

            try
            {
                var comment = mapper.Map<Comment>(request);

                var uploadTasks = new List<Task<string>>();

                if (request.Image != null)
                {
                    comment.ImageUrl = await GetNewNameAndUploadFile(request.Image);
                }

                if (request.TextFile != null)
                {
                    comment.TextFileUrl = await GetNewNameAndUploadFile(request.TextFile);
                }

                var commentJson = JsonConvert.SerializeObject(comment);

                var message = new Message<Null, string> { Value = commentJson };

                await kafkaQueueService.EnqueueMessageAsync(message);

                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing comment.");
                return StatusCode(500, $"Error processing comment: {ex.Message}");
            }
        }
        private async Task<string> GetNewNameAndUploadFile(IFormFile formFile)
        {
            var newFileName = Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName);
            return newFileName;
        }

    }
}
