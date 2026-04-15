using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    public class ExamsController : ControllerBase
    {
        private readonly ExamService _examService;
        private readonly UserService _userService;

        public ExamsController(ExamService examService, UserService userService)
        {
            _examService = examService;
            _userService = userService;
        }

        [Authorize(Roles = "admin")]
        [HttpPost("create")]
        public async Task<IActionResult> Create(ExamCreateDto dto)
        {
            return Ok(await _examService.CreateExamAsync(dto));
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _examService.GetActiveExamsAsync());
        }

        [Authorize]
        [HttpPost("start")]
        public async Task<IActionResult> Start(ExamStartDto dto)
        {
            var userId = _userService.GetUserId();
            return Ok(await _examService.StartAttemptAsync(userId, dto));
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("{id}/release-results")]
        public async Task<IActionResult> Release(int id)
        {
            await _examService.ReleaseResultsAsync(id);
            return NoContent();
        }
    }
}
