using Amazon.Auth.AccessControlPolicy;
using Amazon.S3;
using Amazon.S3.Model;
using CommentApp.Common.Services.FileService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Threading.Tasks;
using Policy = Polly.Policy;

public class AmazonS3FileService(IAmazonS3 amazonS3Client, ILogger<AmazonS3FileService> logger) : IFileService
{
    private readonly IAmazonS3 amazonS3Client = amazonS3Client;
    private const string bucketName = "mycommentsappbucket";

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

    public async Task UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (fileStream == null)
            return;
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType
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

    public async Task<IFormFile> GetFileAsync(string fileName)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = fileName
        };

        using GetObjectResponse response = await amazonS3Client.GetObjectAsync(request);
        using var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        string contentType = response.Headers["Content-Type"] ?? "application/octet-stream";

        var formFile = new FormFile(memoryStream, 0, memoryStream.Length, fileName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
        return formFile;
    }
    public string GeneratePreSignedURL(string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = fileName,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Verb = HttpVerb.GET
        };
        string url = amazonS3Client.GetPreSignedURL(request);
        return url;
    }
}
