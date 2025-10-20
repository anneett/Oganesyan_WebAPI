using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.DTOs
{
    public class SolutionCreateDto
    {
        public int ExerciseId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
    }
}
