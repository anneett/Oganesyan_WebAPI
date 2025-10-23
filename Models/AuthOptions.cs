using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Oganesyan_WebAPI.Models
{
    public class AuthOptions
    {
        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Required]
        public int ExpireMinutes { get; set; }

        [Required]
        public int RefreshTokenExpireDays { get; set; }

        [Required]
        public string Key { get; set; } = string.Empty;
        public SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
    }
}
