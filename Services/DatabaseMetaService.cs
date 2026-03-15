using Humanizer;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class DatabaseMetaService
    {
        private readonly AppDbContext _context;
        public DatabaseMetaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatabaseMeta>> GetAllDatabaseMetasAsync()
        {
            return await _context.DatabaseMetas
                .Include(dm => dm.Deployments!)
                    .ThenInclude(d => d.DbMeta)
                .ToListAsync();
        }

        public async Task<DatabaseMeta?> GetDatabaseMetaByIdAsync(int id)
        {
            return await _context.DatabaseMetas
                .Include(dm => dm.Deployments!)
                    .ThenInclude(d => d.DbMeta)
                .FirstOrDefaultAsync(dm => dm.Id == id);
        }

        public async Task<DatabaseMeta> CreateLogicalDbAsync(DatabaseMetaCreateDto dto, string? erdImagePath)
        {
            var dbMeta = new DatabaseMeta
            {
                LogicalName = dto.LogicalName,
                Description = dto.Description,
                CreateScriptTemplate = dto.CreateScriptTemplate,
                ErdImagePath = erdImagePath,
                CreatedAt = DateTime.UtcNow
            };

            _context.DatabaseMetas.Add(dbMeta);
            await _context.SaveChangesAsync();
            return dbMeta;
        }
    }
}
