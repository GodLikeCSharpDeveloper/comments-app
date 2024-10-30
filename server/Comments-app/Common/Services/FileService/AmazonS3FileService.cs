using Amazon.Auth.AccessControlPolicy;
using Amazon.S3;
using Amazon.S3.Model;
using CommentApp.Common.Services.FileService;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Threading.Tasks;
using Policy = Polly.Policy;

public class AmazonS3FileService(IAmazonS3 amazonS3Client, ILogger logger, string bucketName) : IFileService
{
    private readonly IAmazonS3 amazonS3Client = amazonS3Client;
    private readonly string bucketName = bucketName;

    private readonly AsyncRetryPolicy retryPolicy = Policy
            .Handle<AmazonS3Exception>(ex =>
                ex.StatusCode == HttpStatusCode.InternalServerError ||
                ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            .Or<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                }
            );

    public async Task UploadFileAsync(IFormFile? file, string fileName)
    {
        if (file == null)
            return;

        using var fileStream = file.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = file.ContentType
        };

        var response = await retryPolicy.ExecuteAsync(async () =>
        {
            var res = await amazonS3Client.PutObjectAsync(putRequest);
            if (res.HttpStatusCode != HttpStatusCode.OK)
                throw new AmazonS3Exception("Error while uploading file to S3.")
                {
                    StatusCode = res.HttpStatusCode
                };
            return res;
        });
    }
}
