using CommentApp.Common.AutoMapper;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Services.CaptchaService;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.FileService;
using CommentApp.Common.Services.FileService.FileProcessingService;
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
        ICommentService commentService,
        ICaptchaService captchaService,
        IFileProcessingService fileProcessingService) : ControllerBase
    {
        private readonly ILogger<CommentsController> logger = logger;
        private readonly IAutoMapperService mapper = mapper;
        private readonly IFileService fileService = fileService;
        private readonly IKafkaQueueService kafkaQueueService = kafkaQueueService;
        private readonly ICommentService commentService = commentService;
        private readonly ICaptchaService captchaService = captchaService;
        private readonly IFileProcessingService fileProcessingService = fileProcessingService;

        [HttpPost("post")]
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
                    comment.ImageUrl = fileProcessingService.GetNewNameAndUploadFile(request.Image);
                }
                if (request.TextFile != null)
                {
                    comment.TextFileUrl = fileProcessingService.GetNewNameAndUploadFile(request.TextFile);
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
        [HttpGet("lastComment")]
        public async Task<IActionResult> GetLastAddedCommentForUser([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");
            var lastCommentId = await commentService.GetLastAddedCommentForUser(email);
            return Ok(lastCommentId);
        }
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] CommentQueryParameters queryParameters)
        {
            var response = await commentService.GetCommentsByQueryAsync(queryParameters);
            return Ok(response);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllComments()
        {
            var response = await commentService.GetAllCommentsAsync();
            return Ok(response);
        }

        [HttpGet("count")]
        public async Task<IActionResult> CountComments()
        {
            var response = await commentService.CountAllComments();
            return Ok(response);
        }

        [HttpGet("fileUrl")]
        public async Task<IActionResult> GetPresignedUrl([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest("File path is required.");
            }

            var presignedUrl = fileService.GeneratePreSignedURL(filePath);
            return await Task.FromResult(Ok(presignedUrl));
        }

        [HttpPost("verify")]
        public async Task<IActionResult> ValidateCaptcha([FromQuery] string token)
        {
            var response = await captchaService.ValidateToken(token);
            return Ok(response);
        }
    }
}
