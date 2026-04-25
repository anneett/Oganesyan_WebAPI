using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class ExamAttemptExercise
    {
        public int Id { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }
        public ExamAttempt? ExamAttempt { get; set; }

        [Required]
        public int ExerciseId { get; set; }
        public Exercise? Exercise { get; set; }
        public int OrderIndex { get; set; }
    }
}
