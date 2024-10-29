using Amazon.S3;
using Amazon.S3.Model;

namespace CommentApp.Common.Services.FileService
{
    public class AmazonS3FileService(IAmazonS3 amazonS3Client) : IFileService
    {
        private readonly IAmazonS3 amazonS3Client = amazonS3Client;
        private const string bucketName = "mycommentsappbucket";
        public async Task<string> UploadFileAsync(IFormFile? file)
        {
            if (file == null)
                return string.Empty;
            using var fileStream = file.OpenReadStream();
            var key = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = file.ContentType
            };
            var response = await amazonS3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return key;
            else
                throw new Exception("Error while uploading file into S3.");
        }
    }
}
