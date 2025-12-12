using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public bool InArchive { get; set; } = false;
    }
}
