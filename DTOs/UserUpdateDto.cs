using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserUpdateDto
    {
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;
    }
}
