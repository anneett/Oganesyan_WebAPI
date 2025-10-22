using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class ExerciseCreateDto
    {
        public string Title { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(ExerciseDifficulty))]
        public ExerciseDifficulty Difficulty { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}
