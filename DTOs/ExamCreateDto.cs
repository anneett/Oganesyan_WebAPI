using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class ExamCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required]
        public int DatabaseMetaId { get; set; }

        [Required]
        public int DurationMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public List<int> DeploymentIds { get; set; } = new();
    }
}
