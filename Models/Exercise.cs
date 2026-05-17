using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public enum ExerciseDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }
    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(ExerciseDifficulty))]
        public ExerciseDifficulty Difficulty { get; set; }

        [Required]
        public int DatabaseMetaId { get; set; }
        public DatabaseMeta? DatabaseMeta { get; set; }

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}
