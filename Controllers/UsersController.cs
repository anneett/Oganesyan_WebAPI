using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public UsersController(UserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("add")]
        public async Task<ActionResult<UserDto>> AddUser([FromBody] UserCreateDto userCreateDto)
        {
            try
            {
                var user = await _userService.AddUser(userCreateDto);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new UserDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    UserName = user.UserName,
                    IsAdmin = user.IsAdmin,
                    InArchive = user.InArchive
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
                return NotFound();

            return Ok(new UserDto
            {
                Id = user.Id,
                Login = user.Login,
                UserName = user.UserName,
                IsAdmin = user.IsAdmin,
                InArchive = user.InArchive
            });
        }

        [Authorize(Roles = "admin")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetUsers();
            return Ok(users.Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                UserName = u.UserName,
                IsAdmin = u.IsAdmin,
                InArchive = u.InArchive
            }).ToList());
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            return Ok(await _userService.GetProfile());
        }

        [Authorize(Roles = "admin")]
        [HttpGet("profile/{id}")]
        public async Task<ActionResult<UserDto>> GetProfileById(int id)
        {
            var profile = await _userService.GetUserProfileById(id);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [Authorize]
        [HttpGet("stat")]
        public async Task<ActionResult<IEnumerable<UserSolutionDto>>> GetStatistics([FromQuery] int? databaseMetaId)
        {
            return Ok(await _userService.GetStatistics(databaseMetaId));
        }

        [Authorize(Roles = "admin")]
        [HttpGet("stat/{id}")]
        public async Task<ActionResult<IEnumerable<UserSolutionDto>>> GetStatisticsById(int id, [FromQuery] int? databaseMetaId)
        {
            return Ok(await _userService.GetUserStatisticsById(id, databaseMetaId));
        }

        [Authorize]
        [HttpPatch("update")]
        public async Task<IActionResult> UpdateUserSelf([FromBody] UserUpdateDto userUpdateDto)
        {
            var userId = _userService.GetUserId();
            try
            {
                await _userService.UpdateUser(userId, userUpdateDto);
            }
            catch
            {
                return NotFound();
            }

            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("change/{id}")]
        public async Task<IActionResult> ChangeUserRole(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
                return NotFound();

            await _userService.ChangeUserRole(id);
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("archive/{id}")]
        public async Task<IActionResult> ArchiveUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
                return NotFound();

            await _userService.ArchiveUser(id);
            return NoContent();
        }
    }
}
