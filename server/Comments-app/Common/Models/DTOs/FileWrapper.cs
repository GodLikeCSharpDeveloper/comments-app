namespace CommentApp.Common.Models.DTOs
{
    public class FileWrapper(IFormFile formFile)
    {
        public IFormFile FormFile { get; set; } = formFile;
        public string NewFileName { get => Guid.NewGuid().ToString() + Path.GetExtension(FormFile.FileName); }
    }
}
