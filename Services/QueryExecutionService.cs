using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.Data;
using System.Data.Common;

namespace Oganesyan_WebAPI.Services
{
    public class QueryExecutionService
    {
        private readonly AppDbContext _context;
        private readonly DatabaseDeploymentService _databaseDeploymentService;

        public QueryExecutionService(AppDbContext context, DatabaseDeploymentService databaseDeploymentService)
        {
            _context = context;
            _databaseDeploymentService = databaseDeploymentService;
        }

        public async Task<QueryResultDto> CheckSolutionAsync(ExecuteQueryDto dto)
        {
            if (!IsSafeSelectQuery(dto.UserQuery))
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Разрешены только SELECT-запросы. Запрещены INSERT, UPDATE, DELETE, DROP, ALTER, CREATE и другие."
                };
            }

            var exercise = await _context.Exercises.FirstOrDefaultAsync(e => e.Id == dto.ExerciseId);
            if (exercise == null)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Упражнение не найдено"
                };
            }

            var deployment = await GetDeploymentAsync(dto.DeploymentId);
            if (deployment == null)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Подключение к базе данных не найдено"
                };
            }

            var connectionString = _databaseDeploymentService.BuildConnectionString(
                deployment.DbMeta!,
                deployment.DatabaseMeta!.PhysicalName);

            try
            {
                var userResult = await ExecuteQueryAsync(deployment.DbMeta!.Provider!, connectionString, dto.UserQuery);
                var referenceResult = await ExecuteQueryAsync(deployment.DbMeta.Provider!, connectionString, exercise.CorrectAnswer);
                var isCorrect = CompareDataTables(userResult, referenceResult);

                return new QueryResultDto
                {
                    IsCorrect = isCorrect,
                    Message = isCorrect ? "Запрос выполнен верно!" : BuildMismatchMessage(userResult, referenceResult),
                    UserRowCount = userResult.Rows.Count,
                    UserColumnCount = userResult.Columns.Count,
                    ReferenceRowCount = referenceResult.Rows.Count,
                    ReferenceColumnCount = referenceResult.Columns.Count,
                    ColumnNames = GetColumnNames(userResult),
                    UserRows = DataTableToList(userResult),
                    ReferenceRows = isCorrect ? new List<List<string>>() : DataTableToList(referenceResult)
                };
            }
            catch (Exception ex)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Ошибка выполнения запроса",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<QueryResultDto> ExecutePreviewAsync(int deploymentId, string query)
        {
            if (!IsSafeSelectQuery(query))
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Разрешены только SELECT-запросы."
                };
            }

            var deployment = await GetDeploymentAsync(deploymentId);
            if (deployment == null)
                throw new InvalidOperationException("Подключение к базе данных не найдено");

            var connectionString = _databaseDeploymentService.BuildConnectionString(
                deployment.DbMeta!,
                deployment.DatabaseMeta!.PhysicalName);

            try
            {
                var result = await ExecuteQueryAsync(deployment.DbMeta!.Provider!, connectionString, query);
                return new QueryResultDto
                {
                    IsCorrect = true,
                    Message = "Запрос выполнен успешно",
                    UserRowCount = result.Rows.Count,
                    UserColumnCount = result.Columns.Count,
                    ColumnNames = GetColumnNames(result),
                    UserRows = DataTableToList(result),
                    ReferenceRows = new List<List<string>>()
                };
            }
            catch (Exception ex)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Ошибка выполнения запроса",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<DatabaseDeployment?> GetDeploymentAsync(int deploymentId)
        {
            return await _context.DatabaseDeployments
                .Include(d => d.DbMeta)
                .Include(d => d.DatabaseMeta)
                .FirstOrDefaultAsync(d => d.Id == deploymentId);
        }

        private async Task<DataTable> ExecuteQueryAsync(string provider, string connectionString, string query)
        {
            var factory = DbProviderFactories.GetFactory(provider);
            using var connection = factory.CreateConnection() ?? throw new InvalidOperationException("Не удалось создать подключение");
            connection.ConnectionString = connectionString;

            using var command = connection.CreateCommand() ?? throw new InvalidOperationException("Не удалось создать команду");
            command.CommandText = query;

            var dataTable = new DataTable();

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
        }

        private bool CompareDataTables(DataTable dt1, DataTable dt2)
        {
            if (dt1.Columns.Count != dt2.Columns.Count || dt1.Rows.Count != dt2.Rows.Count)
                return false;

            var sorted1 = dt1.AsEnumerable()
                .Select(r => r.ItemArray.Select(v => v?.ToString() ?? "NULL").ToArray())
                .OrderBy(r => string.Join("|", r))
                .ToList();

            var sorted2 = dt2.AsEnumerable()
                .Select(r => r.ItemArray.Select(v => v?.ToString() ?? "NULL").ToArray())
                .OrderBy(r => string.Join("|", r))
                .ToList();

            for (var i = 0; i < sorted1.Count; i++)
            {
                if (!sorted1[i].SequenceEqual(sorted2[i]))
                    return false;
            }

            return true;
        }

        public bool IsSafeSelectQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var normalized = query.Trim().ToUpperInvariant();
            if (!normalized.StartsWith("SELECT"))
                return false;

            string[] forbidden =
            {
                "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "EXEC",
                "EXECUTE", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "SAVEPOINT", "MERGE", "--", "/*", "XP_"
            };

            foreach (var keyword in forbidden)
            {
                var index = normalized.IndexOf(keyword, StringComparison.Ordinal);
                while (index >= 0)
                {
                    var startOk = index == 0 || !char.IsLetterOrDigit(normalized[index - 1]);
                    var endOk = index + keyword.Length >= normalized.Length || !char.IsLetterOrDigit(normalized[index + keyword.Length]);

                    if (startOk && endOk)
                        return false;

                    index = normalized.IndexOf(keyword, index + 1, StringComparison.Ordinal);
                }
            }

            if (normalized.Contains(';'))
            {
                var withoutTrailingSemicolon = normalized.TrimEnd().TrimEnd(';').TrimEnd();
                if (withoutTrailingSemicolon.Contains(';'))
                    return false;
            }

            return true;
        }

        private string BuildMismatchMessage(DataTable user, DataTable reference)
        {
            if (user.Columns.Count != reference.Columns.Count)
                return $"Количество столбцов не совпадает: у вас {user.Columns.Count}, ожидается {reference.Columns.Count}";

            if (user.Rows.Count != reference.Rows.Count)
                return $"Количество строк не совпадает: у вас {user.Rows.Count}, ожидается {reference.Rows.Count}";

            return "Данные в строках не совпадают с ожидаемым результатом";
        }

        private List<string> GetColumnNames(DataTable dt)
        {
            return dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
        }

        private List<List<string>> DataTableToList(DataTable dt)
        {
            return dt.AsEnumerable()
                .Select(row => row.ItemArray.Select(v => v?.ToString() ?? "NULL").ToList())
                .ToList();
        }
    }
}
