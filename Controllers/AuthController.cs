using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public AuthController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
        }
        public class LoginData
        {
            public string Login { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginData ld)
        {
            var users = await _userService.GetUsers();
            var user = users.FirstOrDefault(u => u.Login == ld.Login);

            if (user == null || !user.CheckPassword(ld.Password))
            {
                return Unauthorized(new { message = "wrong login/password" });
            }

            var token = _authService.GenerateToken(user);
            return Ok(token);
        }
    }
}
