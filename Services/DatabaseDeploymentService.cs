using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class DatabaseDeploymentService
    {
        private readonly AppDbContext _context;
        private readonly DbMetaService _dbMetaService;

        public DatabaseDeploymentService(AppDbContext context, DbMetaService dbMetaService)
        {
            _context = context;
            _dbMetaService = dbMetaService;
        }

        public async Task<List<DatabaseDeployment>> GetDeploymentsByMetaIdAsync(int databaseMetaId)
        {
            return await _context.DatabaseDeployments
                .Include(d => d.DbMeta)
                .Include(d => d.DatabaseMeta)
                .Where(d => d.DatabaseMetaId == databaseMetaId)
                .OrderBy(d => d.DbMeta!.Name)
                .ToListAsync();
        }

        public async Task<DatabaseDeployment> AttachConnectionAsync(int databaseMetaId, DatabaseDeployDto dto)
        {
            var logicalDb = await _context.DatabaseMetas
                .Include(item => item.Deployments)
                .FirstOrDefaultAsync(item => item.Id == databaseMetaId)
                ?? throw new ArgumentException("Логическая БД не найдена");

            var dbMeta = await _context.DbMetas.FindAsync(dto.DbMetaId)
                ?? throw new ArgumentException("Подключение не найдено");

            if (logicalDb.Deployments.Any(item => item.DbMetaId == dto.DbMetaId))
                throw new InvalidOperationException("Это подключение уже привязано к выбранной базе данных");


            var deployment = new DatabaseDeployment
            {
                DatabaseMetaId = databaseMetaId,
                DbMetaId = dto.DbMetaId,
                LinkedAt = DateTime.UtcNow
            };

            _context.DatabaseDeployments.Add(deployment);
            await _context.SaveChangesAsync();
            return deployment;
        }

        public async Task TestDatabaseAvailabilityAsync(DbMeta dbMeta, string databaseName)
        {
            var factory = DbProviderFactories.GetFactory(dbMeta.Provider!);
            using var connection = factory.CreateConnection() ?? throw new InvalidOperationException("Не удалось создать подключение");
            connection.ConnectionString = BuildConnectionString(dbMeta, databaseName);
            await connection.OpenAsync();
        }

        public string BuildConnectionString(DbMeta dbMeta, string databaseName)
        {
            var sourceConnectionString = _dbMetaService.GetDecryptedConnectionString(dbMeta);
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = sourceConnectionString
            };
            builder["Database"] = databaseName;


            if (dbMeta.Provider == "Microsoft.Data.SqlClient")
            {
                builder["Initial Catalog"] = databaseName;
            }
            else if (dbMeta.Provider == "Microsoft.Data.Sqlite")
            {
                builder["Data Source"] = databaseName;
            }
            else
            {
                builder["Database"] = databaseName;
            }

            return builder.ConnectionString;
        }
    }
}
