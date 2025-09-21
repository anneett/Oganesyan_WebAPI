namespace Oganesyan_WebAPI.Models
{
    public class Solution
    {
        //public int Id { get; set; }
        public int UserId { get; set; }
        public int TaskId { get; set; }
        public string UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
