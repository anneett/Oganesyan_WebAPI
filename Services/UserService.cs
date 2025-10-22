using Humanizer;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.Security.Claims;

namespace Oganesyan_WebAPI.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SolutionService _solutionService;

        public UserService(AppDbContext context, IHttpContextAccessor httpContextAccessor, SolutionService solutionService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _solutionService = solutionService;
        }

        public async Task<User> AddUser(UserCreateDto userCreateDto)
        {
            var existing = await GetUserByLogin(userCreateDto.Login);
            if (existing != null) throw new InvalidOperationException("Login already exists.");

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
        private int GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim == null ? throw new UnauthorizedAccessException("Unauthorized user.") : int.Parse(userIdClaim);
        }
        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }
        public async Task<User?> GetUserByLogin(string login)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        }
        public async Task<List<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
        public async Task<UserDto> GetProfile()
        {
            int userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Login = user.Login,
                IsAdmin = user.IsAdmin
            };
            return userDto;
        }
        public async Task<IEnumerable<UserSolutionDto>> GetStatistics()
        {
            int userId = GetUserId();
            return await _solutionService.GetUserSolutionsDetailed(userId);
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
    }
}
