using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class DbMetaService
    {
        private readonly AppDbContext _context;
        private readonly ConnectionStringProtectionService _protectionService;

        public DbMetaService(AppDbContext context, ConnectionStringProtectionService protectionService)
        {
            _context = context;
            _protectionService = protectionService;
        }

        public async Task<List<DbMeta>> GetAllDbMetasAsync()
        {
            return await _context.DbMetas.OrderBy(d => d.Name).ToListAsync();
        }

        public async Task<DbMeta?> GetByIdAsync(int id)
        {
            return await _context.DbMetas.FindAsync(id);
        }

        public async Task<DbMeta> CreateAsync(string name, string dbType, string connectionString, string provider)
        {
            var normalizedName = name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new InvalidOperationException("Имя подключения обязательно");

            var existing = await _context.DbMetas.FirstOrDefaultAsync(d => d.Name.ToLower() == normalizedName.ToLower());
            if (existing != null)
                throw new InvalidOperationException($"Подключение с именем '{normalizedName}' уже существует");

            await TestConnectionAsync(connectionString, provider);

            var dbMeta = new DbMeta
            {
                Name = normalizedName,
                dbType = dbType,
                ConnectionString = _protectionService.Protect(connectionString),
                Provider = provider,
                CreatedAt = DateTime.UtcNow
            };

            _context.DbMetas.Add(dbMeta);
            await _context.SaveChangesAsync();
            return dbMeta;
        }

        public async Task<DbMeta?> UpdateAsync(int id, string name, string dbType, string? connectionString, string provider)
        {
            var dbMeta = await _context.DbMetas.FindAsync(id);
            if (dbMeta == null)
                return null;

            var normalizedName = name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new InvalidOperationException("Имя подключения обязательно");

            var duplicate = await _context.DbMetas
                .FirstOrDefaultAsync(d => d.Id != id && d.Name.ToLower() == normalizedName.ToLower());
            if (duplicate != null)
                throw new InvalidOperationException($"Подключение с именем '{normalizedName}' уже существует");

            var effectiveConnectionString = string.IsNullOrWhiteSpace(connectionString)
                ? _protectionService.Unprotect(dbMeta.ConnectionString)
                : connectionString;

            await TestConnectionAsync(effectiveConnectionString, provider);

            dbMeta.Name = normalizedName;
            dbMeta.dbType = dbType;
            dbMeta.Provider = provider;

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                dbMeta.ConnectionString = _protectionService.Protect(connectionString);
            }

            await _context.SaveChangesAsync();
            return dbMeta;
        }

        public string GetDecryptedConnectionString(DbMeta dbMeta)
        {
            return _protectionService.Unprotect(dbMeta.ConnectionString);
        }

        public string GetMaskedConnectionString(DbMeta dbMeta)
        {
            return _protectionService.MaskProtected(dbMeta);
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
