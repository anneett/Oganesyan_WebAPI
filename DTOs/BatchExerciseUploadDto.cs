using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class BatchExerciseUploadDto
    {
        [Required]
        public int DatabaseMetaId { get; set; }

        public ExerciseDifficulty? DefaultDifficulty { get; set; } = ExerciseDifficulty.Medium;

        [Required]
        public List<BatchExerciseItemDto> Exercises { get; set; } = new();
    }
}
