using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class ExerciseCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(ExerciseDifficulty))]
        public ExerciseDifficulty Difficulty { get; set; }

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}
