﻿using Amazon.S3.Model;
using Amazon.S3;
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
        [HttpGet]
        public IActionResult GetCaptcha()
        {
            var captchaUrl = "";
            return Ok(captchaUrl);

        }

        [HttpPost]
        public IActionResult ValidateCaptcha([FromBody] string userInput)
        {
            var captchaCode = HttpContext.Session.GetString("CaptchaCode");
            return Ok(captchaCode == userInput);
        }

        private string SaveToTempFile(IFormFile formFile)
        {
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                formFile.CopyTo(stream);
            }
            return tempFilePath;
        }

        private string GetNewNameAndUploadFile(IFormFile formFile)
        {
            var newFileName = Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName);
            var tempFilePath = SaveToTempFile(formFile);

            backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
                    await fileService.UploadFileAsync(fileStream, newFileName, formFile.ContentType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while uploading file");
                }
                finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
            });
            return newFileName;
        }


    }
}
