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
    }
}
