using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class DbMetaService
    {
        private readonly AppDbContext _context;
        public DbMetaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DbMeta>> GetAllDbMetasAsync()
        {
            return await _context.DbMetas.ToListAsync();
        }

        //public async Task<DbMeta?> GetByIdAsync(int id)
        //{
        //    return await _context.DbMetas.FindAsync(id);
        //}

        //public async Task<DbMeta?> GetByTypeAsync(string dbType)
        //{
        //    return await _context.DbMetas
        //        .FirstOrDefaultAsync(d => d.dbType == dbType);
        //}

        public async Task<DbMeta> CreateAsync(string dbType, string connectionString, string provider)
        {
            var existing = await _context.DbMetas.FirstOrDefaultAsync(d => d.dbType == dbType);
            if (existing != null)
                throw new InvalidOperationException($"СУБД типа '{dbType}' уже зарегистрирована");

            await TestConnectionAsync(connectionString, provider);

            var dbMeta = new DbMeta
            {
                dbType = dbType,
                ConnectionString = connectionString,
                Provider = provider
            };

            _context.DbMetas.Add(dbMeta);
            await _context.SaveChangesAsync();
            return dbMeta;
        }

        public async Task<bool> TestConnectionAsync(string connectionString, string provider)
        {
            try
            {
                var factory = DbProviderFactories.GetFactory(provider);
                using var connection = factory.CreateConnection() ?? throw new Exception("Не удалось создать подключение");
                connection.ConnectionString = connectionString;
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось подключиться к СУБД: {ex.Message}");
            }
        }

        //public static string GenerateConnectionString(string dbType, string host, int port, string username, string password)
        //{
        //    return dbType switch
        //    {
        //        "PostgreSQL" => $"Host={host};Port={port};Username={username};Password={password}",
        //        "MySQL" => $"Server={host};Port={port};User={username};Password={password}",
        //        "MS SQL Server" => $"Server={host},{port};User Id={username};Password={password};TrustServerCertificate=True",
        //        "SQLite" => "Data Source=:memory:",
        //        _ => throw new NotSupportedException($"СУБД {dbType} не поддерживается")
        //    };
        //}

        public static string GetProviderName(string dbType)
        {
            return dbType switch
            {
                "PostgreSQL" => "Npgsql",
                "MySQL" => "MySqlConnector",
                "MS SQL Server" => "Microsoft.Data.SqlClient",
                "SQLite" => "Microsoft.Data.Sqlite",
                _ => throw new NotSupportedException($"СУБД {dbType} не поддерживается")
            };
        }
    }
}