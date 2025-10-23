using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
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
            public string FrontendHash { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginData ld)
        {
            var user = await _userService.GetUserByLogin(ld.Login);

            if (user == null || !user.CheckPassword(ld.FrontendHash))
            {
                return Unauthorized(new { message = "Wrong login/password." });
            }

            var accessToken = _authService.GenerateAccessToken(user);
            var refreshToken = _authService.GenerateRefreshToken();

            await _userService.SaveRefreshTokenAsync(user.Id, refreshToken);

            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var user = await _userService.GetByRefreshTokenAsync(request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token.");

            var newAccessToken = _authService.GenerateAccessToken(user);
            var newRefreshToken = _authService.GenerateRefreshToken();

            await _userService.SaveRefreshTokenAsync(user.Id, newRefreshToken);

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }
    }
}
