using Microsoft.CodeAnalysis;
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

        public async Task<Models.Solution?> GetSolutionById(int id)
        {
            return await _context.Solutions.FindAsync(id);
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
    }
}
