namespace CommentApp.Common.Services.FileService.FileProcessingService
{
    public interface IFileProcessingService
    {
        string GetNewNameAndUploadFile(IFormFile formFile);
    }
}
