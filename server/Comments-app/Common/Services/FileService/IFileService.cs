namespace CommentApp.Common.Services.FileService
{
    public interface IFileService
    {
        Task UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<IFormFile> GetFileAsync(string fileName);
        string GeneratePreSignedURL(string fileName);
    }
}
