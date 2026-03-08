namespace Oganesyan_WebAPI.DTOs
{
    public class DatabaseMetaCreateDto
    {
        public string LogicalName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CreateScriptTemplate { get; set; }
    }
}
