namespace Oganesyan_WebAPI.DTOs
{
    public class BatchUploadErrorDto
    {
        public int LineNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
