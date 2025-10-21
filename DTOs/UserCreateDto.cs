using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserCreateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
