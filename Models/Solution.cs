using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class Solution
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        [Required]
        public Exercise Exercise { get; set; } = null!;

        [Required]
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Result { get; set; }
    }
}
