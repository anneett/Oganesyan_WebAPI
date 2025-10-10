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

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> AddUser(string login, string password, UserRole userRole)
        {
            var user = await _userService.AddUser(login, password, userRole);
            return Ok();
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
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

        // PUT: api/Users/{id}/make-admin
        [HttpPut("{id}/make-admin")]
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

        // PUT: api/Users/{id}/unmake-admin
        [HttpPut("{id}/unmake-admin")]
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

        // GET: api/Users/{id}/is-admin
        [HttpGet("{id}/is-admin")]
        public async Task<IActionResult> IsAdmin(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userService.IsAdmin(id);

            return Ok();
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
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

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _userService.GetUsers();
        }
        
        //private bool UserExists(int id)
        //{
        //    return _context.Users.Any(e => e.Id == id);
        //}
    }
}
