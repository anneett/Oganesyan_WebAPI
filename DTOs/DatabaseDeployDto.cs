namespace Oganesyan_WebAPI.DTOs
{
    public class DatabaseDeployDto
    {
        public int DbMetaId { get; set; }
        public string PhysicalDatabaseName { get; set; } = string.Empty;
        public bool ExecuteScript { get; set; } = true;
        public string? OneTimeScript { get; set; }
    }
}
