using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserUpdateDto
    {
        public string UserName { get; set; }
        public string Login { get; set; } = string.Empty;

        private byte[] password;
        public string Password
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var b in MD5.Create().ComputeHash(password))
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
            set { password = Encoding.UTF8.GetBytes(value); }
        }
    }
}
