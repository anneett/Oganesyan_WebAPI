using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserSolutionDto
    {
        public int SolutionId { get; set; }
        public int UserId { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        [Required]
        [EnumDataType(typeof(ExerciseDifficulty))]
        public ExerciseDifficulty ExerciseDifficulty { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public DateTime SubmittedAt { get; set; }
        public string? Result { get; set; }
    }
}
