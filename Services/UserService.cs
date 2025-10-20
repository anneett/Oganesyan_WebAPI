using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }
        public async Task<User> AddUser(UserCreateDto userCreateDto)
        {
            var user = new User
            {
                Login = userCreateDto.Login,
                Password = userCreateDto.Password,
                IsAdmin = userCreateDto.IsAdmin
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<bool> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.UserName = userUpdateDto.UserName;
                user.Login = userUpdateDto.Login;
                user.Password = userUpdateDto.Password;

                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> MakeAdmin(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsAdmin = true;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> UnmakeAdmin(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsAdmin = false;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return true;
        }
        public async Task<List<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
