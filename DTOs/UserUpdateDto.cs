namespace Oganesyan_WebAPI.DTOs
{
    public class UserUpdateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
    }
}
