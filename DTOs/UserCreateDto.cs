using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserCreateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
    }
}
