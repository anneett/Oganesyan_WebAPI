using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [Authorize]
        [HttpPost("finish/{examId}")]
        public async Task<IActionResult> FinishExam(int examId)
        {
            var userId = _userService.GetUserId();

            try
            {
                var success = await _examService.FinishExamAsync(userId, examId);

                if (!success)
                    return NotFound("Попытка не найдена");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{examId}/my-results")]
        public async Task<IActionResult> GetMyResults(int examId)
        {
            var userId = _userService.GetUserId();

            var result = await _examService.GetMyResultsAsync(userId, examId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{examId}/attempts")]
        public async Task<IActionResult> GetAttempts(int examId)
        {
            var result = await _examService.GetAttemptsAsync(examId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{examId}/user-info")]
        public async Task<IActionResult> GetUserExamInfo(int examId)
        {
            var userId = _userService.GetUserId();
            var result = await _examService.GetUserExamInfoAsync(userId, examId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
