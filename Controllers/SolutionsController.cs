using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using System.Configuration;
using System.Security.Claims;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolutionsController : ControllerBase
    {
        private readonly SolutionService _solutionService;

        public SolutionsController(SolutionService solutionService)
        {
            _solutionService = solutionService;
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<ActionResult<Solution>> AddSolution(SolutionCreateDto solutionCreateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || userId == 0)
            {
                return Unauthorized();
            }

            var solution = await _solutionService.AddSolution(solutionCreateDto, userId);
            if (solution == null)
            {
                return BadRequest();
            }

            return CreatedAtAction(nameof(GetSolutionById), new { id = solution.Id }, solution);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Solution>> GetSolutionById(int id)
        {
            var solution = await _solutionService.GetSolutionById(id);
            if (solution == null)
            {
                return NotFound();
            }

            return Ok(solution);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Solution>>> GetSolutions()
        {
            return await _solutionService.GetSolutions();
        }

        [Authorize(Roles = "admin")]
        [HttpGet("percentall")]
        public async Task<ActionResult<List<ExerciseStatsDto>>> GetPercentCorrectForAll()
        {
            return await _solutionService.GetExerciseStatsForAll();
        }
    }
}
