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
    public class CommentsController(IProducer<Null, string> kafkaProducer, ILogger<CommentsController> logger, IMapper mapper, IFileService fileService) : ControllerBase
    {
        private readonly IProducer<Null, string> kafkaProducer = kafkaProducer;
        private readonly ILogger<CommentsController> logger = logger;
        private readonly IMapper mapper = mapper;
        private readonly IFileService fileService = fileService;

        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] CommentDto commentDto, IFormFile? textFile, IFormFile? image)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for Comment: {@Comment}", commentDto);
                return BadRequest(ModelState);
            }
            try
            {
                var comment = mapper.Map<Comment>(commentDto);
                comment.ImageUrl = await fileService.UploadFileAsync(image);
                comment.TextFileUrl = await fileService.UploadFileAsync(textFile);
                var commentJson = JsonConvert.SerializeObject(comment);
                await kafkaProducer.ProduceAsync("comments-new", new Message<Null, string> { Value = commentJson });
                //logger.LogInformation("Comment successfully sent to Kafka: {@Comment}", comment);
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending comment to Kafka.");
                return StatusCode(500, $"Error sending message to Kafka: {ex.Message}");
            }
        }
    }
}
