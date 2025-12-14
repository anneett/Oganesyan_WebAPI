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

            if (string.IsNullOrWhiteSpace(solutionCreateDto.UserAnswer))
                throw new ArgumentException("The answer cannot be empty.");

            var solution = new Models.Solution
            {
                UserId = userId,
                ExerciseId = solutionCreateDto.ExerciseId,
                UserAnswer = solutionCreateDto.UserAnswer,
                SubmittedAt = DateTime.UtcNow
            };

            try
            {
                solution.IsCorrect = exercise.CheckAnswer(solutionCreateDto.UserAnswer);
                if (solution.IsCorrect)
                {
                    solution.Result = "OK";
                }
                else
                {
                    solution.Result = $"WrongAnswer: expected {exercise.CorrectAnswer}, got {solution.UserAnswer}";
                }
            }
            catch (Exception ex)
            {
                solution.IsCorrect = false;
                solution.Result = $"Error: {ex.Message}";
            }

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
        public async Task<ExerciseStatsDto?> GetExerciseStatsById(int exerciseId)
        {
            var exercise = await _context.Exercises.FindAsync(exerciseId);
            if (exercise == null)
                return null;

            var solutions = await _context.Solutions
                .Where(s => s.ExerciseId == exerciseId)
                .ToListAsync();

            if (solutions.Count == 0)
                return new ExerciseStatsDto
                {
                    ExerciseId = exerciseId,
                    ExerciseTitle = exercise.Title,
                    TotalAttempts = 0,
                    UniqueUsers = 0,
                    CorrectAnswers = 0,
                    PercentCorrect = 0
                };

            var totalAttempts = solutions.Count;
            var correctAnswers = solutions.Count(s => s.IsCorrect);
            var uniqueUsers = solutions.Select(s => s.UserId).Distinct().Count();

            return new ExerciseStatsDto
            {
                ExerciseId = exerciseId,
                ExerciseTitle = exercise.Title,
                TotalAttempts = totalAttempts,
                UniqueUsers = uniqueUsers,
                CorrectAnswers = correctAnswers,
                PercentCorrect = (double)correctAnswers / totalAttempts * 100.0
            };
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
                    SubmittedAt = s.SubmittedAt,
                    Result = s.Result
                })
                .ToListAsync();

            return query;
        }
        public async Task<List<ExerciseStatsDto>> GetStatsByExercises()
        {
            var stats = await _context.Exercises
                .Select(e => new ExerciseStatsDto
                {
                    ExerciseId = e.Id,
                    ExerciseTitle = e.Title,
                    TotalAttempts = _context.Solutions.Count(s => s.ExerciseId == e.Id),
                    UniqueUsers = _context.Solutions
                        .Where(s => s.ExerciseId == e.Id)
                        .Select(s => s.UserId)
                        .Distinct()
                        .Count(),
                    CorrectAnswers = _context.Solutions
                        .Count(s => s.ExerciseId == e.Id && s.IsCorrect)
                })
                .ToListAsync();

            foreach (var s in stats)
            {
                s.PercentCorrect = s.TotalAttempts == 0 ? 0 : Math.Round((double)s.CorrectAnswers / s.TotalAttempts * 100.0, 2);
            }

            return stats;
        }
        public async Task<List<UserStatsDto>> GetStatsByUsers()
        {
            var stats = await _context.Users
                .Select(u => new UserStatsDto
                {
                    UserId = u.Id,
                    UserLogin = u.Login,
                    TotalAttempts = _context.Solutions.Count(s => s.UserId == u.Id),
                    UniqueExercises = _context.Solutions
                        .Where(s => s.UserId == u.Id)
                        .Select(s => s.ExerciseId)
                        .Distinct()
                        .Count(),
                    CorrectAnswers = _context.Solutions
                        .Count(s => s.UserId == u.Id && s.IsCorrect)
                })
                .ToListAsync();

            foreach (var s in stats)
            {
                s.PercentCorrect = s.TotalAttempts == 0 ? 0 : Math.Round((double)s.CorrectAnswers / s.TotalAttempts * 100.0, 2);
            }

            return stats;
        }
    }
}
