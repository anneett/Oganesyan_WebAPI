namespace Oganesyan_WebAPI.DTOs
{
    public class DeploymentInfoDto
    {
        public int Id { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}
