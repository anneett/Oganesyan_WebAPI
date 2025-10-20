namespace Oganesyan_WebAPI.Models
{
    public class Solution
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
