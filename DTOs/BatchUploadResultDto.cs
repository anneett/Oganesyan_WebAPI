namespace Oganesyan_WebAPI.DTOs
{
    public class BatchUploadResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<BatchUploadErrorDto> Errors { get; set; } = new();
    }
}
