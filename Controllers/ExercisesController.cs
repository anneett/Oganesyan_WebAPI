using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExercisesController : ControllerBase
    {
        private readonly ExerciseService _exerciseService;

        public ExercisesController(ExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<ActionResult<Exercise>> AddExersice([FromBody] ExerciseCreateDto exerciseCreateDto)
        {
            var exercise = await _exerciseService.AddExercise(exerciseCreateDto);
            return CreatedAtAction(nameof(GetExerciseById), new { id = exercise.Id }, exercise);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Exercise>> GetExerciseById(int id)
        {
            var exercise = await _exerciseService.GetExerciseById(id);
            if (exercise == null)
                return NotFound();

            return Ok(exercise);
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Exercise>>> GetExercises([FromQuery] int? databaseMetaId)
        {
            return await _exerciseService.GetExercises(databaseMetaId);
        }

        [Authorize]
        [HttpGet("percent/{id}")]
        public async Task<ActionResult<ExerciseStatsDto?>> GetExerciseStats(int id)
        {
            return await _exerciseService.GetExerciseStatsById(id);
        }

        [Authorize]
        [HttpPost("test-query")]
        public async Task<ActionResult<QueryResultDto>> TestQuery([FromBody] TestQueryDto dto)
        {
            try
            {
                var result = await _exerciseService.TestQueryAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost("batch-upload")]
        public async Task<ActionResult<BatchUploadResultDto>> BatchUploadExercises([FromBody] BatchExerciseUploadDto dto)
        {
            try
            {
                var result = await _exerciseService.BatchUploadExercises(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при загрузке упражнений", details = ex.Message });
            }
        }
    }
}
