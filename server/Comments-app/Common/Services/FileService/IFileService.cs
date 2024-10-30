namespace CommentApp.Common.Services.FileService
{
    public interface IFileService
    {
        Task UploadFileAsync(IFormFile? file, string fileName);
    }
}
