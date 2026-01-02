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

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; private set; } = string.Empty;

        [Required]
        public string Salt { get; private set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public bool InArchive { get; set; } = false;

        [JsonIgnore]
        public string? RefreshToken { get; set; }

        [JsonIgnore]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public long? TelegramChatId { get; set; }

        [JsonIgnore]
        [MaxLength(10)]
        public string? TelegramLinkCode { get; set; }

        [JsonIgnore]
        public DateTime? TelegramLinkCodeExpiry { get; set; }

        public void SetPassword(string frontendHash)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            Salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(frontendHash, saltBytes, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            PasswordHash = Convert.ToBase64String(hash);
        }
        public bool CheckPassword(string frontendHash)
        {
            if (string.IsNullOrEmpty(Salt) || string.IsNullOrEmpty(PasswordHash))
                return false;

            var saltBytes = Convert.FromBase64String(Salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(frontendHash, saltBytes, 100000, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(computedHash) == PasswordHash;
        }
    }
}
