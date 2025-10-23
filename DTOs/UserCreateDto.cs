using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserCreateDto
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}
