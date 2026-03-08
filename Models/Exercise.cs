using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public enum ExerciseDifficulty
    {
        Easy,
        Medium,
        Hard
    }
    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
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
