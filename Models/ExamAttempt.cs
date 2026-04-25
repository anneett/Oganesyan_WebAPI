using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        [Required]
        public int SelectedDeploymentId { get; set; }
        public DatabaseDeployment? SelectedDeployment { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }

        public ICollection<ExamAttemptExercise> SelectedExercises { get; set; } = new List<ExamAttemptExercise>();
    }
}
