namespace Oganesyan_WebAPI.DTOs
{
    public class QueryResultDto
    {
        public bool IsCorrect { get; set; }
        public int UserRowCount { get; set; }
        public int UserColumnCount { get; set; }
        public int ReferenceRowCount { get; set; }
        public int ReferenceColumnCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ColumnNames { get; set; } = new();
        public List<List<string>> UserRows { get; set; } = new();
        public List<List<string>> ReferenceRows { get; set; } = new();
        public string? ErrorDetails { get; set; }
    }
}
