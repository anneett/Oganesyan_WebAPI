using Microsoft.IdentityModel.Tokens;
using Oganesyan_WebAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.Services
{
    public class AuthService
    {
        private readonly AuthOptions _jwtSettings;

        public AuthService(AuthOptions jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }

        public object GenerateAccessToken(User user)
        {
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.Role, user.IsAdmin ? "admin" : "user")
                };

            var jwt = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    notBefore: now,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                    claims: claims,
                    signingCredentials: new SigningCredentials(_jwtSettings.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            return new { token = encodedJwt };
        }
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
