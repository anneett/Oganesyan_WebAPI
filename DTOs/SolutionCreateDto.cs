using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class SolutionCreateDto
    {
        [Required]
        public int ExerciseId { get; set; }
        [Required]
        public string UserAnswer { get; set; } = string.Empty;
    }
}
