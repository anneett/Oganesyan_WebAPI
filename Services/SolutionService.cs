using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.ComponentModel.DataAnnotations;
using Solution = Oganesyan_WebAPI.Models.Solution;

namespace Oganesyan_WebAPI.Services
{
    public class SolutionService
    {
        private readonly AppDbContext _context;
        private readonly QueryExecutionService _queryExecutionService;
        private readonly ExamService _examService;

        public SolutionService(AppDbContext context, QueryExecutionService queryExecutionService, ExamService examService)
        {
            _context = context;
            _queryExecutionService = queryExecutionService;
            _examService = examService;
        }

        public async Task<QueryResultDto?> AddSolution(SolutionCreateDto solutionCreateDto, int userId)
        {
            var exercise = await _context.Exercises.FindAsync(solutionCreateDto.ExerciseId);
            if (exercise == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(solutionCreateDto.UserAnswer))
                throw new ArgumentException("Ответ не может быть пустым.");
            
            if (solutionCreateDto.ExamId.HasValue)
            {
                var isValid = await _examService.IsAttemptValidAsync(userId, solutionCreateDto.ExamId.Value);
                if (!isValid) throw new InvalidOperationException("Время контрольной работы истекло или работа не начата.");
            }

            var executeDto = new ExecuteQueryDto
            {
                ExerciseId = solutionCreateDto.ExerciseId,
                UserQuery = solutionCreateDto.UserAnswer,
                DeploymentId = solutionCreateDto.DeploymentId
            };

            var result = await _queryExecutionService.CheckSolutionAsync(executeDto);

            var solution = new Solution
            {
                UserId = userId,
                ExerciseId = solutionCreateDto.ExerciseId,
                DeploymentId = solutionCreateDto.DeploymentId,
                ExamId = solutionCreateDto.ExamId,
                UserAnswer = solutionCreateDto.UserAnswer,
                IsCorrect = result.IsCorrect,
                SubmittedAt = DateTime.UtcNow,
                Result = result.Message
            };

            _context.Solutions.Add(solution);
            await _context.SaveChangesAsync();

            if (solution.ExamId.HasValue)
            {
                var exam = await _context.Exams.FindAsync(solution.ExamId.Value);
                if (exam != null && !exam.IsResultsReleased)
                {
                    return new QueryResultDto
                    {
                        IsCorrect = false,
                        Message = "Ответ принят и сохранен. Результаты позже.",
                        UserRowCount = result.UserRowCount,
                        UserColumnCount = result.UserColumnCount,
                        ColumnNames = result.ColumnNames,
                        UserRows = result.UserRows,
                        ReferenceRows = new List<List<string>>(),
                        ReferenceRowCount = 0
                    };
                }
            }

            return result;
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
