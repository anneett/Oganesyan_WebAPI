using System.Security.Cryptography.Pkcs;

namespace Oganesyan_WebAPI.DTOs
{
    public class ExerciseStatsDto
    {
        public int ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int UniqueUsers { get; set; }
        public int CorrectAnswers { get; set; }
        public double PercentCorrect { get; set; }
    }
}
