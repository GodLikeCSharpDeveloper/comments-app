namespace CommentApp.Common.Services.FileService
{
    public interface IFileService
    {
        Task<string>UploadFileAsync(IFormFile? file);
    }
}
