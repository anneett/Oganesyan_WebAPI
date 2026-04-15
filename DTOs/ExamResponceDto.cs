namespace Oganesyan_WebAPI.DTOs
{
    public class ExamResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public bool IsResultsReleased { get; set; }
        public string LogicalDbName { get; set; } = string.Empty;
        public List<DeploymentInfoDto> AvailablePlatforms { get; set; } = new();
    }
}
