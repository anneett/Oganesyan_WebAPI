using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
                var result = new UserDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    UserName = user.UserName,
                    IsAdmin = user.IsAdmin,
                    InArchive = user.InArchive
                };

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, result);
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
            {
                return NotFound();
            }
            var userDto = new UserDto
            {
                Id = user.Id,
                Login = user.Login,
                UserName = user.UserName,
                IsAdmin = user.IsAdmin,
                InArchive = user.InArchive
            };

            return Ok(userDto);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetUsers();
            var result = users.Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                UserName = u.UserName,
                IsAdmin = u.IsAdmin,
                InArchive = u.InArchive
            })
            .ToList();

            return Ok(result);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var profile = await _userService.GetProfile();
            return Ok(profile);
        }

        [Authorize]
        [HttpGet("stat")]
        public async Task<ActionResult<UserSolutionDto>> GetStatistics()
        {
            var statistics = await _userService.GetStatistics();
            return Ok(statistics);
        }

        [Authorize]
        [HttpPatch("update")]
        public async Task<IActionResult> UpdateUserSelf([FromBody] UserUpdateDto userUpdateDto)
        {
            int userId = _userService.GetUserId();
            try
            {
                await _userService.UpdateUser(userId, userUpdateDto);
            }
            catch (Exception)
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
            {
                return NotFound();
            }
            await _userService.ChangeUserRole(id);

            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("archive/{id}")]
        public async Task<IActionResult> ArchiveUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userService.ArchiveUser(id);
            return NoContent();
        }

        [Authorize]
        [HttpPost("telegram/generate-link")]
        public async Task<ActionResult<object>> GenerateTelegramLink()
        {
            try
            {
                int userId = _userService.GetUserId();
                var user = await _userService.GetUserById(userId);

                if (user == null)
                    return NotFound();

                if (user.TelegramChatId != null)
                {
                    return BadRequest();
                }

                var code = await _userService.GenerateTelegramLinkCodeAsync(userId);
                var botUsername = _configuration["TelegramBot:BotUsername"];

                return Ok(new
                {
                    deepLink = $"https://t.me/{botUsername}?start={code}",
                    expiresInMinutes = 10
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize]
        [HttpDelete("telegram/unlink")]
        public async Task<IActionResult> UnlinkTelegram()
        {
            int userId = _userService.GetUserId();
            var success = await _userService.UnlinkTelegramAsync(userId);

            if (!success)
                return NotFound();

            return NoContent();
        }

        //[Authorize(Roles = "admin")]
        //[HttpPut("update/{id}")]
        //public async Task<IActionResult> UpdateUserById(int id, [FromBody] UserUpdateDto userUpdateDto)
        //{
        //    try
        //    {
        //        await _userService.UpdateUser(id, userUpdateDto);
        //    }
        //    catch (Exception)
        //    {
        //        return NotFound();
        //    }
        //    return NoContent();
        //}

        //[Authorize(Roles = "admin")]
        //[HttpDelete("delete/{id}")]
        //public async Task<IActionResult> DeleteUserById(int id)
        //{
        //    var user = await _userService.GetUserById(id);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    await _userService.DeleteUser(id);

        //    return NoContent();
        //}

        //[Authorize]
        //[HttpDelete("selfdelete")]
        //public async Task<IActionResult> DeleteUserSelf()
        //{
        //    int userId = _userService.GetUserId();
        //    try
        //    {
        //        await _userService.DeleteUser(userId);
        //    }
        //    catch (Exception)
        //    {
        //        return NotFound();
        //    }

        //    return NoContent();
        //}
    }
}
