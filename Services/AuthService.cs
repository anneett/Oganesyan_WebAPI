using Microsoft.IdentityModel.Tokens;
using Oganesyan_WebAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        public object GenerateToken(User user)
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
                    audience: _jwtSettings.Issuer,
                    notBefore: now,
                    expires: now.AddMinutes(30),
                    claims: claims,
                    signingCredentials: new SigningCredentials(_jwtSettings.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            return new { token = encodedJwt };
        }
    }
}
