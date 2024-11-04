using AutoMapper;
using CommentApp.Common.AutoMapper;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.FileService;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CommentApp.Common.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class CommentsController(ILogger<CommentsController> logger,
        IAutoMapperService mapper, IFileService fileService,
        IKafkaQueueService kafkaQueueService,
        IBackgroundTaskQueue backgroundTaskQueue,
        ICommentService commentService) : ControllerBase
    {
        private readonly ILogger<CommentsController> logger = logger;
        private readonly IAutoMapperService mapper = mapper;
        private readonly IFileService fileService = fileService;
        private readonly IKafkaQueueService kafkaQueueService = kafkaQueueService;
        private readonly IBackgroundTaskQueue backgroundTaskQueue = backgroundTaskQueue;
        private readonly ICommentService commentService = commentService;

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
                var comment = mapper.Map<CreateCommentDto, Comment>(request);

                var uploadTasks = new List<Task<string>>();

                if (request.Image != null)
                {
                    comment.ImageUrl = GetNewNameAndUploadFile(request.Image);
                }
                if (request.TextFile != null)
                {
                    comment.TextFileUrl = GetNewNameAndUploadFile(request.TextFile);
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
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] CommentQueryParameters queryParameters)
        {
            var response = await commentService.GetCommentsByQueryAsync(queryParameters);
            return Ok(response);
        }
        private string GetNewNameAndUploadFile(IFormFile formFile)
        {
            var newFileName = Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName);
            backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    await fileService.UploadFileAsync(formFile, newFileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while uploading file");
                }
            });
            return newFileName;
        }

    }
}
