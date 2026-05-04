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
        public int? DurationMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public List<int> DeploymentIds { get; set; } = new();

        [Required]
        [Range(0, 100)]
        public int EasyCount { get; set; }

        [Required]
        [Range(0, 100)]
        public int MediumCount { get; set; }

        [Required]
        [Range(0, 100)]
        public int HardCount { get; set; }
    }
}
