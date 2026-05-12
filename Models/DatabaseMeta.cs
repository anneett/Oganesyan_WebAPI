using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class DatabaseMeta
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string LogicalName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string PhysicalName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ErdImagePath { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<DatabaseDeployment> Deployments { get; set; } = new List<DatabaseDeployment>();
    }
}
