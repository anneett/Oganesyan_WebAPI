using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;

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
                return null;

            if (string.IsNullOrWhiteSpace(solutionCreateDto.UserAnswer) && !solutionCreateDto.ExamId.HasValue)
            {
                throw new ArgumentException("Ответ не может быть пустым.");
            }

            if (solutionCreateDto.ExamId.HasValue)
            {
                var attempt = await _context.ExamAttempts
                    .Include(a => a.Exam)
                    .FirstOrDefaultAsync(a =>
                        a.UserId == userId &&
                        a.ExamId == solutionCreateDto.ExamId.Value &&
                        a.FinishedAt == null);

                if (attempt == null)
                    throw new InvalidOperationException("Попытка не найдена или уже завершена. Сначала начните экзамен.");

                if (attempt.SelectedDeploymentId != solutionCreateDto.DeploymentId)
                    throw new InvalidOperationException("Используйте подключение, выбранное при начале экзамена");
            }

            QueryResultDto result;
            if (string.IsNullOrWhiteSpace(solutionCreateDto.UserAnswer))
            {
                result = new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Ответ не был предоставлен",
                    UserRowCount = 0,
                    UserColumnCount = 0,
                    ColumnNames = new List<string>(),
                    UserRows = new List<List<string>>(),
                    ReferenceRows = new List<List<string>>()
                };
            }
            else
            {
                result = await _queryExecutionService.CheckSolutionAsync(new ExecuteQueryDto
                {
                    ExerciseId = solutionCreateDto.ExerciseId,
                    UserQuery = solutionCreateDto.UserAnswer,
                    DeploymentId = solutionCreateDto.DeploymentId
                });
            }

            var solution = new Models.Solution
            {
                UserId = userId,
                ExerciseId = solutionCreateDto.ExerciseId,
                DeploymentId = solutionCreateDto.DeploymentId,
                ExamId = solutionCreateDto.ExamId,
                UserAnswer = solutionCreateDto.UserAnswer ?? string.Empty,
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
                        Message = "Ответ принят и сохранен. Результаты будут доступны после проверки.",
                        UserRowCount = result.UserRowCount,
                        UserColumnCount = result.UserColumnCount,
                        ColumnNames = result.ColumnNames,
                        UserRows = result.UserRows,
                        ReferenceRows = new List<List<string>>()
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

            var solutions = await _context.Solutions.Where(s => s.ExerciseId == exerciseId).ToListAsync();
            if (solutions.Count == 0)
            {
                return new ExerciseStatsDto
                {
                    ExerciseId = exerciseId,
                    ExerciseTitle = exercise.Title,
                    ExerciseDifficulty = exercise.Difficulty,
                    DatabaseMetaId = exercise.DatabaseMetaId,
                    TotalAttempts = 0,
                    UniqueUsers = 0,
                    CorrectAnswers = 0,
                    PercentCorrect = 0
                };
            }

            var totalAttempts = solutions.Count;
            var correctAnswers = solutions.Count(s => s.IsCorrect);
            var uniqueUsers = solutions.Select(s => s.UserId).Distinct().Count();

            return new ExerciseStatsDto
            {
                ExerciseId = exerciseId,
                ExerciseTitle = exercise.Title,
                ExerciseDifficulty = exercise.Difficulty,
                DatabaseMetaId = exercise.DatabaseMetaId,
                TotalAttempts = totalAttempts,
                UniqueUsers = uniqueUsers,
                CorrectAnswers = correctAnswers,
                PercentCorrect = (double)correctAnswers / totalAttempts * 100.0
            };
        }

        public async Task<IEnumerable<UserSolutionDto>> GetUserSolutionsDetailed(int userId, int? databaseMetaId = null)
        {
            var query = _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId);

            if (databaseMetaId.HasValue)
            {
                query = query.Where(s => s.Exercise.DatabaseMetaId == databaseMetaId.Value);
            }

            return await query
                .Select(s => new UserSolutionDto
                {
                    SolutionId = s.Id,
                    UserId = s.UserId,
                    ExerciseId = s.ExerciseId,
                    DatabaseMetaId = s.Exercise.DatabaseMetaId,
                    ExerciseTitle = s.Exercise.Title,
                    ExerciseDifficulty = s.Exercise.Difficulty,
                    CorrectAnswer = s.Exercise.CorrectAnswer,
                    UserAnswer = s.UserAnswer,
                    IsCorrect = s.IsCorrect,
                    SubmittedAt = s.SubmittedAt,
                    Result = s.Result,
                    IsExam = s.ExamId.HasValue
                })
                .ToListAsync();
        }

        public async Task<List<ExerciseStatsDto>> GetStatsByExercises(int? databaseMetaId = null)
        {
            var exercisesQuery = _context.Exercises.AsQueryable();
            if (databaseMetaId.HasValue)
            {
                exercisesQuery = exercisesQuery.Where(e => e.DatabaseMetaId == databaseMetaId.Value);
            }

            var stats = await exercisesQuery
                .Select(e => new ExerciseStatsDto
                {
                    ExerciseId = e.Id,
                    ExerciseTitle = e.Title,
                    ExerciseDifficulty = e.Difficulty,
                    DatabaseMetaId = e.DatabaseMetaId,
                    TotalAttempts = _context.Solutions.Count(s => s.ExerciseId == e.Id),
                    UniqueUsers = _context.Solutions.Where(s => s.ExerciseId == e.Id).Select(s => s.UserId).Distinct().Count(),
                    CorrectAnswers = _context.Solutions.Count(s => s.ExerciseId == e.Id && s.IsCorrect)
                })
                .ToListAsync();

            foreach (var item in stats)
            {
                item.PercentCorrect = item.TotalAttempts == 0 ? 0 : Math.Round((double)item.CorrectAnswers / item.TotalAttempts * 100.0, 2);
            }

            return stats;
        }

        public async Task<List<UserStatsDto>> GetStatsByUsers(int? databaseMetaId = null)
        {
            var stats = await _context.Users
                .Select(u => new UserStatsDto
                {
                    UserId = u.Id,
                    UserLogin = u.Login,
                    TotalAttempts = databaseMetaId.HasValue
                        ? _context.Solutions.Count(s => s.UserId == u.Id && s.Exercise.DatabaseMetaId == databaseMetaId.Value)
                        : _context.Solutions.Count(s => s.UserId == u.Id),
                    UniqueExercises = databaseMetaId.HasValue
                        ? _context.Solutions.Where(s => s.UserId == u.Id && s.Exercise.DatabaseMetaId == databaseMetaId.Value).Select(s => s.ExerciseId).Distinct().Count()
                        : _context.Solutions.Where(s => s.UserId == u.Id).Select(s => s.ExerciseId).Distinct().Count(),
                    CorrectAnswers = databaseMetaId.HasValue
                        ? _context.Solutions.Count(s => s.UserId == u.Id && s.Exercise.DatabaseMetaId == databaseMetaId.Value && s.IsCorrect)
                        : _context.Solutions.Count(s => s.UserId == u.Id && s.IsCorrect)
                })
                .ToListAsync();

            foreach (var item in stats)
            {
                item.PercentCorrect = item.TotalAttempts == 0 ? 0 : Math.Round((double)item.CorrectAnswers / item.TotalAttempts * 100.0, 2);
            }

            return stats;
        }
    }
}
