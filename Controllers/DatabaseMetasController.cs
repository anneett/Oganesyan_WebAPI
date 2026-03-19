using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
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
        public async Task<ActionResult<IEnumerable<DatabaseMeta>>> GetAllDatabaseMetas()
        {
            return await _databaseMetaService.GetAllDatabaseMetasAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<DatabaseMeta?>>> GetDatabaseMetaById(int id)
        {
            var databaseMeta = await _databaseMetaService.GetDatabaseMetaByIdAsync(id);

            if (databaseMeta == null)
            {
                return NotFound(new { message = "Логическая БД не найдена" });
            }
            return Ok(databaseMeta);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddLogicalDatabase([FromForm] DatabaseMetaCreateDto dto, IFormFile? erdImage)
        {
            string? erdImagePath = null;

            if (erdImage != null)
            {
                var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "erd");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}" + $"{Path.GetExtension(erdImage.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await erdImage.CopyToAsync(stream);

                erdImagePath = $"/uploads/erd/{fileName}";
            }

            var databaseMeta = await _databaseMetaService.CreateLogicalDbAsync(dto, erdImagePath);

            return Ok(new
            {
                databaseMeta.Id,
                databaseMeta.LogicalName
            });
        }
    }
}
