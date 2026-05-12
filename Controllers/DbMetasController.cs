using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbMetasController : ControllerBase
    {
        private readonly DbMetaService _dbMetaService;

        public DbMetasController(DbMetaService dbMetaService)
        {
            _dbMetaService = dbMetaService;
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<ActionResult> GetAllDbMetas()
        {
            var dbMetas = await _dbMetaService.GetAllDbMetasAsync();
            var result = dbMetas.Select(d => new
            {
                d.Id,
                d.Name,
                d.dbType,
                d.Provider,
                d.CreatedAt,
                MaskedConnectionString = _dbMetaService.GetMaskedConnectionString(d)
            });

            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<IActionResult> CreateDbMeta([FromBody] DbMetaCreateDto dto)
        {
            try
            {
                var provider = DbMetaService.GetProviderName(dto.DbType);
                var dbMeta = await _dbMetaService.CreateAsync(dto.Name, dto.DbType, dto.ConnectionString, provider);

                return Ok(new
                {
                    dbMeta.Id,
                    dbMeta.Name,
                    dbMeta.dbType,
                    dbMeta.Provider,
                    dbMeta.CreatedAt,
                    MaskedConnectionString = _dbMetaService.GetMaskedConnectionString(dbMeta)
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDbMeta(int id, [FromBody] DbMetaUpdateDto dto)
        {
            try
            {
                var provider = DbMetaService.GetProviderName(dto.DbType);
                var updated = await _dbMetaService.UpdateAsync(id, dto.Name, dto.DbType, dto.ConnectionString, provider);

                if (updated == null)
                    return NotFound(new { message = "Подключение не найдено" });

                return Ok(new
                {
                    updated.Id,
                    updated.Name,
                    updated.dbType,
                    updated.Provider,
                    updated.CreatedAt,
                    MaskedConnectionString = _dbMetaService.GetMaskedConnectionString(updated)
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost("test")]
        public async Task<IActionResult> TestConnection([FromBody] DbMetaCreateDto dto)
        {
            try
            {
                var provider = DbMetaService.GetProviderName(dto.DbType);
                await _dbMetaService.TestConnectionAsync(dto.ConnectionString, provider);
                return Ok(new { success = true, message = "Подключение успешно" });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}
