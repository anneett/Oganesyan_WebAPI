using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;

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

        [Authorize(Roles = "admin")]
        [HttpGet("get-solution/{id}")]
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
        [HttpPost("add-solution")]
        public async Task<ActionResult<Solution>> AddSolution(int userId, string userAnswer, Exercise exercise)
        {
            var solution = await _solutionService.AddSolution(userId, userAnswer, exercise);
            return CreatedAtAction("GetSolutionById", new { id = solution.Id }, solution);
        }
    }
}
