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

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("add-user")]
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
                    IsAdmin = user.IsAdmin
                };

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet("get-user-by-id/{id}")]
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
                IsAdmin = user.IsAdmin
            };

            return Ok(userDto);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("get-users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetUsers();
            var result = users.Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                UserName = u.UserName,
                IsAdmin = u.IsAdmin
            })
            .ToList();

            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-profile")]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var profile = await _userService.GetProfile();
            return Ok(profile);
        }

        [Authorize]
        [HttpGet("get-statistics")]
        public async Task<ActionResult<UserSolutionDto>> GetStatistics()
        {
            var statistics = await _userService.GetStatistics();
            return Ok(statistics);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUserById(int id, [FromBody] UserUpdateDto userUpdateDto)
        {
            try
            {
                await _userService.UpdateUser(id, userUpdateDto);
            }
            catch (Exception)
            {
                return NotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpPut("update-user-self")]
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
        [HttpPut("make-admin/{id}/make-admin")]
        public async Task<IActionResult> MakeUserAdmin(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userService.MakeAdmin(id);

            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPut("unmake-admin/{id}/unmake-admin")]
        public async Task<IActionResult> UnmakeUserAdmin(int id)
        {
            int userId = _userService.GetUserId();
            if (userId == id)
            {
                return BadRequest("You cannot unadminister yourself.");
            }

            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userService.UnmakeAdmin(id);

            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userService.DeleteUser(id);

            return NoContent();
        }

        [Authorize]
        [HttpDelete("delete-user-self")]
        public async Task<IActionResult> DeleteUserSelf()
        {
            int userId = _userService.GetUserId();
            try
            {
                await _userService.DeleteUser(userId);
            }
            catch (Exception)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
