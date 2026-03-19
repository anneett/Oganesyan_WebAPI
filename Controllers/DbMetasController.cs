using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using System.Data;

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
        public async Task<ActionResult<IEnumerable<DbMeta>>> GetAllDbMetas()
        {
            var dbMetas = await _dbMetaService.GetAllDbMetasAsync();

            var result = dbMetas.Select(d => new
            {
                d.Id,
                d.dbType,
                d.Provider
            });

            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<ActionResult<DbMeta>> CreateDbMeta([FromBody] DbMetaCreateDto dto)
        {
            try
            {
                var provider = DbMetaService.GetProviderName(dto.DbType);
                var dbMeta = await _dbMetaService.CreateAsync(dto.DbType, dto.ConnectionString, provider);

                return Ok(new
                {
                    dbMeta.Id,
                    dbMeta.dbType,
                    dbMeta.Provider
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
