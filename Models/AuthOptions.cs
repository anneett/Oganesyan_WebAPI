using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Oganesyan_WebAPI.Models
{
    public class AuthOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; }
        public int RefreshTokenExpireDays { get; set; }
        public string Key { get; set; } = string.Empty;
        public SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
    }
}
