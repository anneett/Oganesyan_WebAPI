namespace Oganesyan_WebAPI.Models
{
    public enum UserRole
    {
        Student,
        Admin
    }
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
        public string Email { get; set; }

        //Statistics
    }
}
