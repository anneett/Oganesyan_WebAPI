using System.ComponentModel.DataAnnotations;

namespace Oganesyan_WebAPI.DTOs
{
    public class TestQueryDto
    {
        [Required]
        public int DeploymentId { get; set; }

        [Required]
        public string Query { get; set; } = string.Empty;
    }
}
