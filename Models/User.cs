namespace Oganesyan_WebAPI.Models
{
    public enum UserRole
    {
        User,
        Admin
    }
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }

        //Statistics
    }
}
