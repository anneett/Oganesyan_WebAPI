using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.DTOs;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class DatabaseDeploymentService
    {
        private readonly AppDbContext _context;
        public DatabaseDeploymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatabaseDeployment>> GetDeploymentsByMetaIdAsync(int databaseMetaId)
        {
            return await _context.DatabaseDeployments
                .Include(d => d.DbMeta)
                .Where(d => d.DatabaseMetaId == databaseMetaId)
                .ToListAsync();
        }

        public async Task<DatabaseDeployment> DeployDatabaseAsync(int databaseMetaId, DatabaseDeployDto dto)
        {
            var logicalDb = await _context.DatabaseMetas
                .FindAsync(databaseMetaId) ?? throw new ArgumentException("Логическая БД не найдена");

            var dbMeta = await _context.DbMetas
                .FindAsync(dto.DbMetaId) ?? throw new ArgumentException("СУБД не найдена");

            var existing = await _context.DatabaseDeployments
                .FirstOrDefaultAsync(d =>
                    d.DatabaseMetaId == databaseMetaId &&
                    d.DbMetaId == dto.DbMetaId);

            if (existing != null)
                throw new InvalidOperationException("Эта БД уже развернута на данной СУБД");

            bool deployed = false;

            if (dto.ExecuteScript && !string.IsNullOrEmpty(logicalDb.CreateScriptTemplate))
            {
                await CreatePhysicalDatabase(dbMeta, dto.PhysicalDatabaseName);
                string adaptedScript = AdaptSqlScript(logicalDb.CreateScriptTemplate, dbMeta.dbType);

                await ExecuteScriptOnDatabase(dbMeta, dto.PhysicalDatabaseName, adaptedScript);
            
                deployed = true;
            }

            var deployment = new DatabaseDeployment
            {
                DatabaseMetaId = databaseMetaId,
                DbMetaId = dto.DbMetaId,
                PhysicaDatabaseName = dto.PhysicalDatabaseName,
                IsDeployed = deployed,
                DeployedAt = deployed ? DateTime.UtcNow : default
            };

            _context.DatabaseDeployments.Add(deployment);
            await _context.SaveChangesAsync();

            return deployment;
        }

        private async Task CreatePhysicalDatabase(DbMeta dbMeta, string databaseName)
        {
            var factory = DbProviderFactories.GetFactory(dbMeta.Provider!);
            using var connection = factory.CreateConnection()!;
            connection.ConnectionString = GetServerConnectionString(dbMeta);

            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = dbMeta.dbType switch
            {
                "PostgreSQL" => $"CREATE DATABASE \"{databaseName}\"",
                "MySQL" => $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
                "MS SQL Server" => $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}') CREATE DATABASE [{databaseName}]",
                _ => throw new NotSupportedException($"СУБД {dbMeta.dbType} не поддерживается")
            };

            await command.ExecuteNonQueryAsync();
        }

        private async Task ExecuteScriptOnDatabase(DbMeta dbMeta, string databaseName, string script)
        {
            var factory = DbProviderFactories.GetFactory(dbMeta.Provider!);
            using var connection = factory.CreateConnection()!;
            connection.ConnectionString = AddDatabaseToConnectionString(dbMeta.ConnectionString, databaseName);

            await connection.OpenAsync();

            var statements = script
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var statement in statements)
            {
                using var command = connection.CreateCommand();
                command.CommandText = statement;
                await command.ExecuteNonQueryAsync();
            }
        }

        private string AdaptSqlScript(string script, string dbType)
        {
            return dbType switch
            {
                "PostgreSQL" => script
                    .Replace("INT PRIMARY KEY AUTO_INCREMENT", "SERIAL PRIMARY KEY")
                    .Replace("AUTO_INCREMENT", "")
                    .Replace("INTEGER PRIMARY KEY AUTOINCREMENT", "SERIAL PRIMARY KEY"),

                "MySQL" => script
                    .Replace("SERIAL PRIMARY KEY", "INT PRIMARY KEY AUTO_INCREMENT")
                    .Replace("INTEGER PRIMARY KEY AUTOINCREMENT", "INT PRIMARY KEY AUTO_INCREMENT"),

                "MS SQL Server" => script
                    .Replace("SERIAL PRIMARY KEY", "INT IDENTITY(1,1) PRIMARY KEY")
                    .Replace("AUTO_INCREMENT", "")
                    .Replace("INTEGER PRIMARY KEY AUTOINCREMENT", "INT IDENTITY(1,1) PRIMARY KEY"),

                "SQLite" => script
                    .Replace("SERIAL PRIMARY KEY", "INTEGER PRIMARY KEY AUTOINCREMENT")
                    .Replace("INT PRIMARY KEY AUTO_INCREMENT", "INTEGER PRIMARY KEY AUTOINCREMENT"),

                _ => script
            };
        }

        private string GetServerConnectionString(DbMeta dbMeta)
        {
            return dbMeta.dbType switch
            {
                "PostgreSQL" => AddDatabaseToConnectionString(dbMeta.ConnectionString, "postgres"),
                "MySQL" => dbMeta.ConnectionString,
                "MS SQL Server" => AddDatabaseToConnectionString(dbMeta.ConnectionString, "master"),
                _ => dbMeta.ConnectionString
            };
        }

        public string AddDatabaseToConnectionString(string connectionString, string databaseName)
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            builder["Database"] = databaseName;
            return builder.ConnectionString;
        }
    }
}
