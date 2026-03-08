using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.Models
{
    public class DatabaseDeployment
    {
        public int Id { get; set; }

        [Required]
        public int DatabaseMetaId { get; set; }
        public DatabaseMeta? DatabaseMeta { get; set; }

        [Required]
        public int DbMetaId { get; set; }
        public DbMeta? DbMeta { get; set; }

        [Required]
        public string PhysicaDatabaseName { get; set; } = string.Empty;
        public bool IsDeployed { get; set; }
        public DateTime DeployedAt { get; set; } 
    }
}
