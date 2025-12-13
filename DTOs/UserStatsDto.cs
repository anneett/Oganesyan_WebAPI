namespace Oganesyan_WebAPI.DTOs
{
    public class UserStatsDto
    {
        public int UserId { get; set; }
        public string UserLogin { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int UniqueExercises { get; set; }
        public int CorrectAnswers { get; set; }
        public double PercentCorrect { get; set; }
    }
}
