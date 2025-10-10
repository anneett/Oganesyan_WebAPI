using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // GET: api/Users/get-user-by-id/{id}
        [HttpGet("get-user-by-id/{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/Users/add-user
        [HttpPost("add-user")]
        public async Task<ActionResult<User>> AddUser(string login, string password, UserRole userRole)
        {
            var user = await _userService.AddUser(login, password, userRole);
            return Ok(user);
        }

        // PUT: api/Users/update-user/{id}
        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }
            try
            {
                await _userService.UpdateUser(user);
            }
            catch (Exception)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Users/make-admin/{id}/make-admin
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

        // PUT: api/Users/unmake-admin/{id}/unmake-admin
        [HttpPut("unmake-admin/{id}/unmake-admin")]
        public async Task<IActionResult> UnmakeUserAdmin(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userService.UnmakeAdmin(id);

            return NoContent();
        }

        // DELETE: api/Users/delete-user/{id}
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userService.DeleteUser(id);

            return NoContent();
        }

        // GET: api/Users/get-users
        [HttpGet("get-users")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _userService.GetUsers();
        }
    }
}
