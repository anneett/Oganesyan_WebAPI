namespace Oganesyan_WebAPI.DTOs
{
    public class DbMetaUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
        public string? ConnectionString { get; set; }
    }
}
