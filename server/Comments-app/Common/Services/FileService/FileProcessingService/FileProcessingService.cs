namespace CommentApp.Common.Services.FileService.FileProcessingService
{
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IFileService fileService;
        private readonly IBackgroundTaskQueue backgroundTaskQueue;
        private readonly ILogger<FileProcessingService> logger;

        public FileProcessingService(IFileService fileService, IBackgroundTaskQueue backgroundTaskQueue, ILogger<FileProcessingService> logger)
        {
            this.fileService = fileService;
            this.backgroundTaskQueue = backgroundTaskQueue;
            this.logger = logger;
        }

        public string SaveToTempFile(IFormFile formFile)
        {
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                formFile.CopyTo(stream);
            }
            return tempFilePath;
        }

        public string GetNewNameAndUploadFile(IFormFile formFile)
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
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            });
            return newFileName;
        }
    }

}
