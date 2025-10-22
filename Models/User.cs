using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Oganesyan_WebAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Salt { get; private set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;

        [JsonIgnore]
        public string? RefreshToken { get; set; }

        [JsonIgnore]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public void SetPassword(string rawPassword)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            Salt = Convert.ToBase64String(saltBytes);

            var combined = Encoding.UTF8.GetBytes(rawPassword + Salt);

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(combined);
            PasswordHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public bool CheckPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(Salt) || string.IsNullOrEmpty(PasswordHash))
                return false;

            var combined = Encoding.UTF8.GetBytes(rawPassword + Salt);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(combined);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return hash == PasswordHash;
        }
    }
}
