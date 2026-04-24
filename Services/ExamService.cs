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

        public async Task<Exam> CreateExamAsync(ExamCreateDto dto)
        {
            var deployments = await _context.DatabaseDeployments
                .Where(d => dto.DeploymentIds.Contains(d.Id))
                .ToListAsync();

            if (deployments.Any(d => d.DatabaseMetaId != dto.DatabaseMetaId))
                throw new InvalidOperationException("Некоторые развертывания не принадлежат выбранной логической БД");

            if (deployments.Count != dto.DeploymentIds.Count)
                throw new InvalidOperationException("Некоторые развертывания не найдены");
            
            var exam = new Exam
            {
                Title = dto.Title,
                Description = dto.Description,
                DatabaseMetaId = dto.DatabaseMetaId,
                DurationMinutes = dto.DurationMinutes,
                MaxAttempts = dto.MaxAttempts,
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
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == dto.ExamId && a.FinishedAt == null);

            if (unfinished != null)
                return unfinished;

            var completedAttempts = await _context.ExamAttempts
                .Where(a => a.UserId == userId && a.ExamId == dto.ExamId && a.FinishedAt != null)
                .CountAsync();

            if (exam.MaxAttempts.HasValue && completedAttempts >= exam.MaxAttempts.Value)
                throw new InvalidOperationException($"Вы исчерпали все попытки ({exam.MaxAttempts.Value})");

            var attempt = new ExamAttempt
            {
                UserId = userId,
                ExamId = dto.ExamId,
                SelectedDeploymentId = dto.DeploymentId,
                StartedAt = DateTime.UtcNow
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt;
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
                CanStart = !exam.MaxAttempts.HasValue || completedCount < exam.MaxAttempts.Value || hasUnfinished
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
                var solutions = await _context.Solutions
                    .Where(s => s.UserId == userId && s.ExamId == examId)
                    .ToListAsync();

                var attemptSolutions = solutions
                    .Where(s => s.SubmittedAt >= attempt.StartedAt && s.SubmittedAt <= (attempt.FinishedAt ?? DateTime.UtcNow))
                    .ToList();

                var correctCount = attemptSolutions.Count(s => s.IsCorrect);
                var totalCount = attemptSolutions.Count;

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
                return new
                {
                    IsResultsReleased = false,
                    Message = "Результаты будут доступны после проверки преподавателем",
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt
                };
            }

            var solutions = await _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId && s.ExamId == exam.Id)
                .Where(s => s.SubmittedAt >= attempt.StartedAt && s.SubmittedAt <= (attempt.FinishedAt ?? DateTime.UtcNow))
                .ToListAsync();

            return new
            {
                IsResultsReleased = true,
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                FinishedAt = attempt.FinishedAt,
                Solutions = solutions.Select(s => new
                {
                    s.ExerciseId,
                    ExerciseTitle = s.Exercise.Title,
                    s.UserAnswer,
                    s.IsCorrect,
                    s.Result
                }),
                CorrectAnswers = solutions.Count(s => s.IsCorrect),
                TotalExercises = solutions.Count
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

            var solutions = await _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId && s.ExamId == examId)
                .Where(s => s.SubmittedAt >= attempt.StartedAt &&
                            s.SubmittedAt <= (attempt.FinishedAt ?? DateTime.UtcNow))
                .ToListAsync();

            if (!exam.IsResultsReleased)
            {
                return new
                {
                    IsResultsReleased = false,
                    Message = "Результаты будут доступны после проверки",
                    SubmittedCount = solutions.Count,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt
                };
            }

            return new
            {
                IsResultsReleased = true,
                StartedAt = attempt.StartedAt,
                FinishedAt = attempt.FinishedAt,
                Solutions = solutions.Select(s => new
                {
                    s.ExerciseId,
                    ExerciseTitle = s.Exercise.Title,
                    s.UserAnswer,
                    s.IsCorrect,
                    s.Result
                }),
                CorrectAnswers = solutions.Count(s => s.IsCorrect),
                TotalExercises = solutions.Count
            };
        }
        public async Task<List<object>> GetAttemptsAsync(int examId)
        {
            var attempts = await _context.ExamAttempts
                .Include(a => a.User)
                .Where(a => a.ExamId == examId)
                .Select(a => new
                {
                    a.Id,
                    UserId = a.User!.Id,
                    UserLogin = a.User.Login,
                    UserName = a.User.UserName,
                    a.StartedAt,
                    a.FinishedAt,
                    CorrectAnswers = _context.Solutions.Count(s => s.UserId == a.UserId && s.ExamId == examId && s.IsCorrect),
                    TotalAnswers = _context.Solutions.Count(s => s.UserId == a.UserId && s.ExamId == examId)
                })
                .ToListAsync<object>();

            return attempts;
        }
    }
}
