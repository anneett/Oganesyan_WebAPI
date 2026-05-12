using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Oganesyan_WebAPI.Models
{
    public class DbMeta
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string dbType { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        [JsonIgnore]
        public string ConnectionString { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Provider { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
