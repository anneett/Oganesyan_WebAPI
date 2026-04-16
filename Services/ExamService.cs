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
            var existing = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == dto.ExamId);

            if (existing != null) return existing;

            var exam = await _context.Exams
                .Include(e => e.AvailableDeployments)
                .FirstOrDefaultAsync(e => e.Id == dto.ExamId && e.IsActive);

            if (exam == null)
                throw new InvalidOperationException("Экзамен не найден или неактивен");

            if (!exam.AvailableDeployments.Any(ad => ad.DatabaseDeploymentId == dto.DeploymentId))
                throw new InvalidOperationException("Выбранное развертывание недоступно для этого экзамена");

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

        public async Task<bool> IsAttemptValidAsync(int userId, int examId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == examId);

            if (attempt == null) return false;
            if (attempt.FinishedAt != null) return false;

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
        public async Task<bool> FinishExamAsync(int userId, int examId)
        {
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == examId);

            if (attempt == null)
                return false;

            if (attempt.FinishedAt != null)
                throw new InvalidOperationException("Экзамен уже завершен");

            attempt.FinishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<object?> GetMyResultsAsync(int userId, int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return null;

            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ExamId == examId);

            if (attempt == null)
                throw new InvalidOperationException("Вы не начинали этот экзамен");

            var solutions = await _context.Solutions
                .Include(s => s.Exercise)
                .Where(s => s.UserId == userId && s.ExamId == examId)
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
