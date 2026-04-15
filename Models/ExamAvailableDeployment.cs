using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class ExamAvailableDeployment
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        [Required]
        public int DatabaseDeploymentId { get; set; }
        public DatabaseDeployment? DatabaseDeployment { get; set; }
    }
}
