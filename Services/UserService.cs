using Humanizer;
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
        public async Task<User?> GetUserByLogin(string login)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        }
        public async Task<User> AddUser(UserCreateDto userCreateDto)
        {
            var existing = await GetUserByLogin(userCreateDto.Login);
            if (existing != null) throw new InvalidOperationException("Login already exists");

            var user = new User
            {
                UserName = userCreateDto.UserName,
                Login = userCreateDto.Login
            };

            user.SetPassword(userCreateDto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<bool> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(userUpdateDto.UserName))
                    user.UserName = userUpdateDto.UserName;

                if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
                    user.SetPassword(userUpdateDto.Password);

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
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
