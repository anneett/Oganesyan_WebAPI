using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
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

        public async Task<Models.Solution?> AddSolution(SolutionCreateDto solutionCreateDto, int userId)
        {
            var exercise = await _context.Exercises.FindAsync(solutionCreateDto.ExerciseId);
            if (exercise == null)
            {
                return null;
            }

            var solution = new Models.Solution
            {
                UserId = userId,
                ExerciseId = solutionCreateDto.ExerciseId,
                UserAnswer = solutionCreateDto.UserAnswer,
                IsCorrect = exercise.CheckAnswer(solutionCreateDto.UserAnswer),
                SubmittedAt = DateTime.UtcNow
            };

            _context.Solutions.Add(solution);
            await _context.SaveChangesAsync();

            return solution;
        }
        public async Task<Models.Solution?> GetSolutionById(int id)
        {
            return await _context.Solutions.FindAsync(id);
        }

        public async Task<List<Models.Solution>> GetSolutions()
        {
            return await _context.Solutions.ToListAsync();
        }
        public async Task<double> GetPercentCorrectById(int exerciseId)
        {
            var stats = await _context.Solutions
                .Where(s => s.ExerciseId == exerciseId)
                .GroupBy(s => s.ExerciseId)
                .Select(g => new
                {
                    Total = g.Count(),
                    Correct = g.Count(s => s.IsCorrect)
                })
                .FirstOrDefaultAsync();

                if (stats == null || stats.Total == 0)
                    return 0;

                return (double)stats.Correct / stats.Total * 100.0;
        }
        public async Task<Dictionary<int, double>> GetPercentCorrectForAll()
        {
            var stats = await _context.Solutions
                .GroupBy(s => s.ExerciseId)
                .Select(g => new
                {
                    ExerciseId = g.Key,
                    Total = g.Count(),
                    Correct = g.Count(s => s.IsCorrect)
                })
                .ToListAsync();

            return stats.ToDictionary(
                x => x.ExerciseId,
                x => (double)x.Correct / x.Total * 100.0
             );
        }
        public async Task<IEnumerable<UserSolutionDto>> GetUserSolutionsDetailed(int userId)
        {
            var query = await _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId)
                .Select(s => new UserSolutionDto
                {
                    SolutionId = s.Id,
                    ExerciseId = s.ExerciseId,
                    ExerciseTitle = s.Exercise.Title,
                    ExerciseDifficulty = s.Exercise.Difficulty,
                    CorrectAnswer = s.Exercise.CorrectAnswer,
                    UserAnswer = s.UserAnswer,
                    IsCorrect = s.IsCorrect,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();

            return query;
        }
    }
}
