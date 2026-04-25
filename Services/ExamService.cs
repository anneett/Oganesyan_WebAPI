using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class ExamService
    {
        private readonly AppDbContext _context;

        public ExamService(AppDbContext context)
        {
            _context = context;
        }

        private async Task<List<ExamAttemptExercise>> GetAttemptExerciseMappingsAsync(int attemptId)
        {
            return await _context.ExamAttemptExercises
                .Include(ae => ae.Exercise)
                .Where(ae => ae.ExamAttemptId == attemptId)
                .OrderBy(ae => ae.OrderIndex)
                .ToListAsync();
        }

        private async Task<List<Solution>> GetAttemptSolutionsAsync(int userId, int examId, ExamAttempt attempt)
        {
            var attemptExerciseIds = await _context.ExamAttemptExercises
                .Where(ae => ae.ExamAttemptId == attempt.Id)
                .Select(ae => ae.ExerciseId)
                .ToListAsync();

            return await _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId && s.ExamId == examId)
                .Where(s => attemptExerciseIds.Contains(s.ExerciseId))
                .Where(s => s.SubmittedAt >= attempt.StartedAt && s.SubmittedAt <= (attempt.FinishedAt ?? DateTime.UtcNow))
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        private static List<Solution> GetLatestSolutionsPerExercise(IEnumerable<Solution> solutions)
        {
            return solutions
                .GroupBy(s => s.ExerciseId)
                .Select(group => group
                    .OrderByDescending(s => s.SubmittedAt)
                    .ThenByDescending(s => s.Id)
                    .First())
                .ToList();
        }

        public async Task<Exam> CreateExamAsync(ExamCreateDto dto)
        {
            var deployments = await _context.DatabaseDeployments
                .Where(d => dto.DeploymentIds.Contains(d.Id))
                .ToListAsync();

            if (deployments.Any(d => d.DatabaseMetaId != dto.DatabaseMetaId))
                throw new InvalidOperationException("Некоторые развертывания не принадлежат выбранной логической БД");

            if (deployments.Count != dto.DeploymentIds.Count)
                throw new InvalidOperationException("Некоторые развертывания не найдены");

            var easyAvailable = await _context.Exercises
                .Where(e => e.DatabaseMetaId == dto.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Easy)
                .CountAsync();

            var mediumAvailable = await _context.Exercises
                .Where(e => e.DatabaseMetaId == dto.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Medium)
                .CountAsync();

            var hardAvailable = await _context.Exercises
                .Where(e => e.DatabaseMetaId == dto.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Hard)
                .CountAsync();

            if (easyAvailable < dto.EasyCount)
                throw new InvalidOperationException($"Недостаточно легких заданий. Доступно: {easyAvailable}, запрошено: {dto.EasyCount}");

            if (mediumAvailable < dto.MediumCount)
                throw new InvalidOperationException($"Недостаточно средних заданий. Доступно: {mediumAvailable}, запрошено: {dto.MediumCount}");

            if (hardAvailable < dto.HardCount)
                throw new InvalidOperationException($"Недостаточно сложных заданий. Доступно: {hardAvailable}, запрошено: {dto.HardCount}");

            var exam = new Exam
            {
                Title = dto.Title,
                Description = dto.Description,
                DatabaseMetaId = dto.DatabaseMetaId,
                DurationMinutes = dto.DurationMinutes,
                MaxAttempts = dto.MaxAttempts,
                EasyCount = dto.EasyCount,
                MediumCount = dto.MediumCount,
                HardCount = dto.HardCount,
                IsActive = true,
                IsResultsReleased = false
            };

            foreach (var depId in dto.DeploymentIds)
            {
                exam.AvailableDeployments.Add(new ExamAvailableDeployment { DatabaseDeploymentId = depId });
            }

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            return exam;
        }

        public async Task<List<ExamResponseDto>> GetActiveExamsAsync()
        {
            return await _context.Exams
                .Where(e => e.IsActive)
                .Select(e => new ExamResponseDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    DurationMinutes = e.DurationMinutes,
                    DatabaseMetaId = e.DatabaseMetaId,
                    MaxAttempts = e.MaxAttempts,
                    IsActive = e.IsActive,
                    IsResultsReleased = e.IsResultsReleased,
                    LogicalDbName = e.DatabaseMeta!.LogicalName,
                    EasyCount = e.EasyCount,
                    MediumCount = e.MediumCount,
                    HardCount = e.HardCount,
                    TotalExercises = e.EasyCount + e.MediumCount + e.HardCount,
                    AvailablePlatforms = e.AvailableDeployments.Select(ad => new DeploymentInfoDto
                    {
                        Id = ad.DatabaseDeploymentId,
                        DbType = ad.DatabaseDeployment!.DbMeta!.dbType,
                        Provider = ad.DatabaseDeployment.DbMeta.Provider ?? ""
                    }).ToList()
                }).ToListAsync();
        }

        public async Task<ExamAttempt> StartAttemptAsync(int userId, ExamStartDto dto)
        {
            var exam = await _context.Exams
                .Include(e => e.AvailableDeployments)
                .FirstOrDefaultAsync(e => e.Id == dto.ExamId && e.IsActive);

            if (exam == null)
                throw new InvalidOperationException("Экзамен не найден или неактивен");

            if (exam.IsResultsReleased)
                throw new InvalidOperationException("Результаты этой контрольной уже опубликованы. Новые попытки недоступны.");

            if (!exam.AvailableDeployments.Any(ad => ad.DatabaseDeploymentId == dto.DeploymentId))
                throw new InvalidOperationException("Выбранное развертывание недоступно для этого экзамена");

            var unfinished = await _context.ExamAttempts
                .Include(a => a.SelectedExercises)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == dto.ExamId && a.FinishedAt == null);

            if (unfinished != null)
                return unfinished;

            var completedAttempts = await _context.ExamAttempts
                .Where(a => a.UserId == userId && a.ExamId == dto.ExamId && a.FinishedAt != null)
                .CountAsync();

            if (exam.MaxAttempts.HasValue && completedAttempts >= exam.MaxAttempts.Value)
                throw new InvalidOperationException($"Вы исчерпали все попытки ({exam.MaxAttempts.Value})");

            var random = new Random();

            var allEasy = await _context.Exercises
                .Where(e => e.DatabaseMetaId == exam.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Easy)
                .ToListAsync();
            var easyExercises = allEasy.OrderBy(_ => random.Next()).Take(exam.EasyCount).ToList();

            var allMedium = await _context.Exercises
                .Where(e => e.DatabaseMetaId == exam.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Medium)
                .ToListAsync();
            var mediumExercises = allMedium.OrderBy(_ => random.Next()).Take(exam.MediumCount).ToList();

            var allHard = await _context.Exercises
                .Where(e => e.DatabaseMetaId == exam.DatabaseMetaId && e.Difficulty == ExerciseDifficulty.Hard)
                .ToListAsync();
            var hardExercises = allHard.OrderBy(_ => random.Next()).Take(exam.HardCount).ToList();

            var selectedExercises = easyExercises
                .Concat(mediumExercises)
                .Concat(hardExercises)
                .OrderBy(_ => random.Next())
                .ToList();

            var attempt = new ExamAttempt
            {
                UserId = userId,
                ExamId = dto.ExamId,
                SelectedDeploymentId = dto.DeploymentId,
                StartedAt = DateTime.UtcNow
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            for (int i = 0; i < selectedExercises.Count; i++)
            {
                _context.ExamAttemptExercises.Add(new ExamAttemptExercise
                {
                    ExamAttemptId = attempt.Id,
                    ExerciseId = selectedExercises[i].Id,
                    OrderIndex = i
                });
            }

            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task<List<Exercise>> GetAttemptExercisesAsync(int attemptId)
        {
            return await _context.ExamAttemptExercises
                .Where(ae => ae.ExamAttemptId == attemptId)
                .OrderBy(ae => ae.OrderIndex)
                .Select(ae => ae.Exercise!)
                .ToListAsync();
        }

        public async Task<object> GetUserExamInfoAsync(int userId, int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return null!;

            var attempts = await _context.ExamAttempts
                .Where(a => a.UserId == userId && a.ExamId == examId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            var completedCount = attempts.Count(a => a.FinishedAt != null);
            var hasUnfinished = attempts.Any(a => a.FinishedAt == null);

            return new
            {
                ExamId = examId,
                MaxAttempts = exam.MaxAttempts,
                CompletedAttempts = completedCount,
                RemainingAttempts = exam.MaxAttempts.HasValue ? exam.MaxAttempts.Value - completedCount : (int?)null,
                HasUnfinishedAttempt = hasUnfinished,
                CanStart = !exam.MaxAttempts.HasValue || completedCount < exam.MaxAttempts.Value || hasUnfinished,
                UsedAttempts = attempts.Count
            };
        }

        public async Task<bool> IsAttemptValidAsync(int userId, int examId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId &&
                    a.ExamId == examId &&
                    a.FinishedAt == null);

            if (attempt == null) return false;

            var endTime = attempt.StartedAt.AddMinutes(attempt.Exam!.DurationMinutes);
            return DateTime.UtcNow <= endTime;
        }

        public async Task ReleaseResultsAsync(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                exam.IsResultsReleased = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<object> GetUserAllAttemptsAsync(int userId, int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null)
                throw new InvalidOperationException("Экзамен не найден");

            var attempts = await _context.ExamAttempts
                .Where(a => a.UserId == userId && a.ExamId == examId && a.FinishedAt != null)
                .OrderByDescending(a => a.FinishedAt)
                .ToListAsync();

            if (attempts.Count == 0)
            {
                return new
                {
                    ExamId = examId,
                    ExamTitle = exam.Title,
                    IsResultsReleased = exam.IsResultsReleased,
                    Attempts = new List<object>(),
                    Message = "Вы еще не завершили ни одной попытки"
                };
            }

            var attemptsWithResults = new List<object>();

            foreach (var attempt in attempts)
            {
                var attemptExercises = await GetAttemptExerciseMappingsAsync(attempt.Id);
                var attemptSolutions = await GetAttemptSolutionsAsync(userId, examId, attempt);
                var latestSolutions = GetLatestSolutionsPerExercise(attemptSolutions);

                var correctCount = latestSolutions.Count(s => s.IsCorrect);
                var totalCount = attemptExercises.Count;

                attemptsWithResults.Add(new
                {
                    AttemptId = attempt.Id,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt,
                    CorrectAnswers = correctCount,
                    TotalAnswers = totalCount,
                    Percentage = totalCount > 0 ? Math.Round((double)correctCount / totalCount * 100, 1) : 0
                });
            }

            var bestAttempt = attemptsWithResults
                .OrderByDescending(a => ((dynamic)a).CorrectAnswers)
                .ThenByDescending(a => ((dynamic)a).Percentage)
                .FirstOrDefault();

            return new
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                IsResultsReleased = exam.IsResultsReleased,
                MaxAttempts = exam.MaxAttempts,
                UsedAttempts = attempts.Count,
                Attempts = attemptsWithResults,
                BestAttemptId = bestAttempt != null ? ((dynamic)bestAttempt).AttemptId : (int?)null
            };
        }

        public async Task<object?> GetAttemptDetailsAsync(int userId, int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

            if (attempt == null)
                throw new InvalidOperationException("Попытка не найдена");

            var exam = attempt.Exam!;

            if (!exam.IsResultsReleased)
            {
                var submittedExerciseIds = await GetAttemptSolutionsAsync(userId, exam.Id, attempt);

                return new
                {
                    IsResultsReleased = false,
                    Message = "Результаты будут доступны после проверки преподавателем",
                    SubmittedCount = submittedExerciseIds
                        .Select(s => s.ExerciseId)
                        .Distinct()
                        .Count(),
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt
                };
            }

            var attemptExercises = await GetAttemptExerciseMappingsAsync(attempt.Id);
            var latestSolutions = GetLatestSolutionsPerExercise(await GetAttemptSolutionsAsync(userId, exam.Id, attempt));

            return new
            {
                IsResultsReleased = true,
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                FinishedAt = attempt.FinishedAt,
                Solutions = latestSolutions
                    .OrderBy(solution =>
                        attemptExercises.FindIndex(exercise => exercise.ExerciseId == solution.ExerciseId))
                    .Select(s => new
                    {
                        s.ExerciseId,
                        ExerciseTitle = s.Exercise.Title,
                        s.UserAnswer,
                        s.IsCorrect,
                        s.Result
                    }),
                CorrectAnswers = latestSolutions.Count(s => s.IsCorrect),
                TotalExercises = attemptExercises.Count
            };
        }

        public async Task<bool> FinishExamAsync(int userId, int examId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId &&
                    a.ExamId == examId &&
                    a.FinishedAt == null);

            if (attempt == null)
                throw new InvalidOperationException("Активная попытка не найдена");

            attempt.FinishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<object?> GetMyResultsAsync(int userId, int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return null;

            var attempt = await _context.ExamAttempts
                .Where(a => a.UserId == userId && a.ExamId == examId)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync();

            if (attempt == null)
                throw new InvalidOperationException("Вы не начинали этот экзамен");

            var attemptExercises = await GetAttemptExerciseMappingsAsync(attempt.Id);
            var attemptSolutions = await GetAttemptSolutionsAsync(userId, examId, attempt);
            var latestSolutions = GetLatestSolutionsPerExercise(attemptSolutions);

            if (!exam.IsResultsReleased)
            {
                return new
                {
                    IsResultsReleased = false,
                    Message = "Результаты будут доступны после проверки",
                    SubmittedCount = latestSolutions.Count,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt
                };
            }

            return new
            {
                IsResultsReleased = true,
                StartedAt = attempt.StartedAt,
                FinishedAt = attempt.FinishedAt,
                Solutions = latestSolutions
                    .OrderBy(solution =>
                        attemptExercises.FindIndex(exercise => exercise.ExerciseId == solution.ExerciseId))
                    .Select(s => new
                    {
                        s.ExerciseId,
                        ExerciseTitle = s.Exercise.Title,
                        s.UserAnswer,
                        s.IsCorrect,
                        s.Result
                    }),
                CorrectAnswers = latestSolutions.Count(s => s.IsCorrect),
                TotalExercises = attemptExercises.Count
            };
        }
        public async Task<List<Exercise>?> GetAttemptExercisesForUserAsync(int userId, int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

            if (attempt == null)
                return null;

            var exercises = await _context.ExamAttemptExercises
                .Where(ae => ae.ExamAttemptId == attemptId)
                .OrderBy(ae => ae.OrderIndex)
                .Select(ae => ae.Exercise!)
                .ToListAsync();

            return exercises;
        }

        public async Task<List<object>> GetAttemptsAsync(int examId)
        {
            var attempts = await _context.ExamAttempts
                .Include(a => a.User)
                .Where(a => a.ExamId == examId && a.FinishedAt != null)
                .ToListAsync();

            var result = new List<object>();

            foreach (var attempt in attempts)
            {
                var attemptExercises = await GetAttemptExerciseMappingsAsync(attempt.Id);
                var latestSolutions = GetLatestSolutionsPerExercise(
                    await GetAttemptSolutionsAsync(attempt.UserId, examId, attempt));

                result.Add(new
                {
                    attempt.Id,
                    UserId = attempt.User!.Id,
                    UserLogin = attempt.User.Login,
                    UserName = attempt.User.UserName,
                    attempt.StartedAt,
                    attempt.FinishedAt,
                    CorrectAnswers = latestSolutions.Count(s => s.IsCorrect),
                    TotalAnswers = attemptExercises.Count
                });
            }

            return result;
        }
    }
}
