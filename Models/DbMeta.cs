using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class DbMeta
    {
        public int Id { get; set; }

        [Required]
        public string dbType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ConnectionString { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Provider { get; set; }
    }
}