namespace Oganesyan_WebAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Email { get; set; }
    }
}
