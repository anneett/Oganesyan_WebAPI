namespace Oganesyan_WebAPI.DTOs
{
    public class ExecuteQueryDto
    {
        public int ExerciseId { get; set; }
        public string UserQuery { get; set; } = string.Empty;
        public int DeploymentId { get; set; }
    }
}
