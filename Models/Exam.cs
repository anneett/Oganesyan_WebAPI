using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required]
        public int DatabaseMetaId { get; set; }
        public DatabaseMeta? DatabaseMeta { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsResultsReleased { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<ExamAvailableDeployment> AvailableDeployments { get; set; } = new List<ExamAvailableDeployment>();
    }
}
