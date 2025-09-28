namespace Oganesyan_WebAPI.Models
{
    public class Solution
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; }
        public string UserSQL { get; set; }
        public string UserAnswer { get; set; }
        public bool IsCorrect { get; set; } // => Exercise.CheckAnswer(UserAnswer);
        public DateTime SubmittedAt { get; set; }
    }
}
