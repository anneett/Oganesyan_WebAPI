namespace Oganesyan_WebAPI.Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Difficulty { get; set; }
        public string CorrectAnswer { get; set; }

        public bool CheckAnswer(string userAnswer)
        {
            return string.Equals(userAnswer.Trim(), CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public string ShowAnswer()
        {
            return CorrectAnswer;
        }

        // сохранить ответ/засчитать правильный ответ/сохранить неправильный ответ??? работа с классом solutions
    }
}
