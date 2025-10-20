using Microsoft.AspNetCore.Identity.Data;
using Newtonsoft.Json.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.Models
{
    public class User
    {
        public int Id { get; set; }
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
        public bool IsAdmin { get; set; } = false;

        public bool CheckPassword(string rawPassword)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString() == Password;
        }

        // +-: просмотр профиля/просмотреть статистику
    }
}
