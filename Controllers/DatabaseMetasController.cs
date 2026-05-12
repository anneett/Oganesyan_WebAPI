using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseMetasController : ControllerBase
    {
        private readonly DatabaseMetaService _databaseMetaService;
        private readonly IWebHostEnvironment _env;

        public DatabaseMetasController(DatabaseMetaService databaseMetaService, IWebHostEnvironment env)
        {
            _databaseMetaService = databaseMetaService;
            _env = env;
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllDatabaseMetas()
        {
            var result = await _databaseMetaService.GetAllDatabaseMetasAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDatabaseMetaById(int id)
        {
            var databaseMeta = await _databaseMetaService.GetDatabaseMetaByIdAsync(id);
            if (databaseMeta == null)
                return NotFound(new { message = "База данных не найдена" });

            return Ok(databaseMeta);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddLogicalDatabase([FromForm] DatabaseMetaCreateDto dto, IFormFile? erdImage)
        {
            try
            {
                var erdImagePath = await SaveErdImageAsync(erdImage);
                var databaseMeta = await _databaseMetaService.CreateLogicalDbAsync(dto, erdImagePath);

                return Ok(new
                {
                    databaseMeta.Id,
                    databaseMeta.LogicalName,
                    databaseMeta.PhysicalName
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogicalDatabase(int id, [FromForm] DatabaseMetaUpdateDto dto, IFormFile? erdImage)
        {
            try
            {
                var newErdImagePath = await SaveErdImageAsync(erdImage);
                var updated = await _databaseMetaService.UpdateLogicalDbAsync(id, dto, newErdImagePath);

                if (updated == null)
                    return NotFound(new { message = "База данных не найдена" });

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<string?> SaveErdImageAsync(IFormFile? erdImage)
        {
            if (erdImage == null)
                return null;

            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "erd");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(erdImage.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await erdImage.CopyToAsync(stream);

            return $"/uploads/erd/{fileName}";
        }
    }
}
