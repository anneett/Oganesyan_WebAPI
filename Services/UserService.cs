using Humanizer;
using Microsoft.AspNetCore.Authentication.OAuth;
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
        private readonly AuthOptions _authOptions;

        public UserService(AppDbContext context, IHttpContextAccessor httpContextAccessor, SolutionService solutionService, AuthOptions authOptions)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _solutionService = solutionService;
            _authOptions = authOptions;
        }

        public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_authOptions.RefreshTokenExpireDays);
                await _context.SaveChangesAsync();
            }
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

            if (!_context.Users.Any())
            {
                user.IsAdmin = true;
            }

            user.SetPassword(userCreateDto.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public int GetUserId()
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
        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }
        public async Task<UserDto> GetProfile()
        {
            int userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Login = user.Login,
                IsAdmin = user.IsAdmin,
                InArchive = user.InArchive,
                IsTelegramLinked = user.TelegramChatId != null
            };
            return userDto;
        }
        public async Task<IEnumerable<UserSolutionDto>> GetStatistics()
        {
            int userId = GetUserId();
            return await _solutionService.GetUserSolutionsDetailed(userId);
        }
        public async Task<User?> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(userUpdateDto.UserName))
                    user.UserName = userUpdateDto.UserName;

                await _context.SaveChangesAsync();
                return user;
            }
            return null;
        }
        public async Task<bool> ChangeUserRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsAdmin = !user.IsAdmin;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> ArchiveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.InArchive = !user.InArchive;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<string> GenerateTelegramLinkCodeAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            user.TelegramLinkCode = code;
            user.TelegramLinkCodeExpiry = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();
            return code;
        }

        public async Task<bool> LinkTelegramAsync(string code, long telegramChatId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.TelegramLinkCode == code &&
                u.TelegramLinkCodeExpiry > DateTime.UtcNow);

            if (user == null)
                return false;

            user.TelegramChatId = telegramChatId;

            user.TelegramLinkCode = null;
            user.TelegramLinkCodeExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByTelegramChatIdAsync(long chatId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
        }

        public async Task<bool> UnlinkTelegramAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.TelegramChatId = null;
            user.TelegramLinkCode = null;
            user.TelegramLinkCodeExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }

        //public async Task<bool> DeleteUser(int id)
        //{
        //    var user = await _context.Users.FindAsync(id);
        //    if (user == null) return false;

        //    _context.Users.Remove(user);
        //    await _context.SaveChangesAsync();
        //    return true;
        //}
    }
}
