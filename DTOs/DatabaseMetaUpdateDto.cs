using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class DatabaseMetaUpdateDto
    {
        [Required]
        public string LogicalName { get; set; } = string.Empty;

        [Required]
        public string PhysicalName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public List<int> ConnectionIds { get; set; } = new();

        public bool RemoveErdImage { get; set; } = false;
    }
}
