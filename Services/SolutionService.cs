using Microsoft.CodeAnalysis;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Services
{
    public class SolutionService
    {
        private readonly AppDbContext _context;
        public SolutionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Models.Solution?> GetSolutionById(int id)
        {
            return await _context.Solutions.FindAsync(id);
        }
        public async Task<bool> AddSolution(int userId, string userAnswer, Exercise exercise)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                var solution = new Models.Solution
                {
                    UserId = userId,
                    ExerciseId = exercise.Id,
                    UserAnswer = userAnswer,
                    IsCorrect = exercise.CheckAnswer(userAnswer),
                    SubmittedAt = DateTime.UtcNow
                };
                _context.Solutions.Add(solution);
                await _context.SaveChangesAsync();

                return true;
            }
            return false;
        }
    }
}
