using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class DatabaseMetaService
    {
        private readonly AppDbContext _context;
        private readonly DatabaseDeploymentService _deploymentService;

        public DatabaseMetaService(AppDbContext context, DatabaseDeploymentService deploymentService)
        {
            _context = context;
            _deploymentService = deploymentService;
        }

        public async Task<List<DatabaseMeta>> GetAllDatabaseMetasAsync()
        {
            return await _context.DatabaseMetas
                .Include(dm => dm.Deployments)
                    .ThenInclude(d => d.DbMeta)
                .OrderBy(dm => dm.LogicalName)
                .ToListAsync();
        }

        public async Task<DatabaseMeta?> GetDatabaseMetaByIdAsync(int id)
        {
            return await _context.DatabaseMetas
                .Include(dm => dm.Deployments)
                    .ThenInclude(d => d.DbMeta)
                .FirstOrDefaultAsync(dm => dm.Id == id);
        }

        public async Task<DatabaseMeta> CreateLogicalDbAsync(DatabaseMetaCreateDto dto, string? erdImagePath)
        {
            ValidateDatabaseMeta(dto.LogicalName, dto.PhysicalName, dto.Description);

            var connectionIds = dto.ConnectionIds.Distinct().ToList();

            var existingConnections = await _context.DbMetas
                .Where(connection => connectionIds.Contains(connection.Id))
                .ToListAsync();

            if (existingConnections.Count != connectionIds.Count)
                throw new InvalidOperationException("Некоторые выбранные подключения не найдены");

            foreach (var connection in existingConnections)
            {
                try
                {
                    await _deploymentService.TestDatabaseAvailabilityAsync(connection, dto.PhysicalName);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"На подключении '{connection.Name}' не найдена база данных '{dto.PhysicalName}'. Проверьте физическое имя.");
                }
            }

            var dbMeta = new DatabaseMeta
            {
                LogicalName = dto.LogicalName.Trim(),
                PhysicalName = dto.PhysicalName.Trim(),
                Description = dto.Description.Trim(),
                ErdImagePath = erdImagePath,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var connectionId in connectionIds)
            {
                dbMeta.Deployments.Add(new DatabaseDeployment
                {
                    DbMetaId = connectionId,
                    LinkedAt = DateTime.UtcNow
                });
            }

            _context.DatabaseMetas.Add(dbMeta);
            await _context.SaveChangesAsync();
            return dbMeta;
        }

        public async Task<DatabaseMeta?> UpdateLogicalDbAsync(int id, DatabaseMetaUpdateDto dto, string? newErdImagePath)
        {
            ValidateDatabaseMeta(dto.LogicalName, dto.PhysicalName, dto.Description);

            var dbMeta = await _context.DatabaseMetas
                .Include(meta => meta.Deployments)
                .FirstOrDefaultAsync(meta => meta.Id == id);

            if (dbMeta == null)
                return null;

            var connectionIds = dto.ConnectionIds.Distinct().ToList();
            var existingConnections = await _context.DbMetas
                .Where(connection => connectionIds.Contains(connection.Id))
                .ToListAsync();

            if (existingConnections.Count != connectionIds.Count)
                throw new InvalidOperationException("Некоторые выбранные подключения не найдены");

            foreach (var connection in existingConnections)
            {
                try
                {
                    await _deploymentService.TestDatabaseAvailabilityAsync(connection, dto.PhysicalName);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"На подключении '{connection.Name}' не найдена база данных '{dto.PhysicalName}'. Проверьте физическое имя.");
                }
            }

            dbMeta.LogicalName = dto.LogicalName.Trim();
            dbMeta.PhysicalName = dto.PhysicalName.Trim();
            dbMeta.Description = dto.Description.Trim();

            if (dto.RemoveErdImage)
            {
                dbMeta.ErdImagePath = null;
            }
            else if (newErdImagePath != null)
            {
                dbMeta.ErdImagePath = newErdImagePath;
            }

            var currentDeployments = dbMeta.Deployments.ToList();
            var currentConnectionIds = currentDeployments.Select(deployment => deployment.DbMetaId).ToHashSet();

            foreach (var deployment in currentDeployments.Where(deployment => !connectionIds.Contains(deployment.DbMetaId)))
            {
                await EnsureDeploymentCanBeDetachedAsync(deployment.Id);
                _context.DatabaseDeployments.Remove(deployment);
            }

            foreach (var connectionId in connectionIds.Where(connectionId => !currentConnectionIds.Contains(connectionId)))
            {
                dbMeta.Deployments.Add(new DatabaseDeployment
                {
                    DbMetaId = connectionId,
                    LinkedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return dbMeta;
        }

        private static void ValidateDatabaseMeta(string logicalName, string physicalName, string description)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                throw new InvalidOperationException("Название базы данных обязательно");

            if (string.IsNullOrWhiteSpace(physicalName))
                throw new InvalidOperationException("Физическое имя базы данных обязательно");

            if (string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException("Описание базы данных обязательно");
        }

        private async Task EnsureDeploymentCanBeDetachedAsync(int deploymentId)
        {
            var isUsedInExamSettings = await _context.ExamAvailableDeployments.AnyAsync(item => item.DatabaseDeploymentId == deploymentId);
            if (isUsedInExamSettings)
                throw new InvalidOperationException("Нельзя отвязать подключение от базы данных, пока оно используется в контрольных работах");

            var isUsedInAttempts = await _context.ExamAttempts.AnyAsync(item => item.SelectedDeploymentId == deploymentId);
            if (isUsedInAttempts)
                throw new InvalidOperationException("Нельзя отвязать подключение от базы данных, пока по нему есть попытки контрольных работ");

            var isUsedInSolutions = await _context.Solutions.AnyAsync(item => item.DeploymentId == deploymentId);
            if (isUsedInSolutions)
                throw new InvalidOperationException("Нельзя отвязать подключение от базы данных, пока по нему есть решения студентов");
        }
    }
}
