using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using System.Data;
using System.Data.Common;
using System.Globalization;

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

            var userDeployment = await GetDeploymentAsync(dto.DeploymentId);
            if (userDeployment == null)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Подключение к базе данных не найдено"
                };
            }

            var referenceDeployment = await ResolveReferenceDeploymentAsync(exercise, userDeployment);
            if (referenceDeployment == null)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = exercise.ReferenceDbType == null
                        ? "Не удалось подобрать подключение для эталонного ответа"
                        : $"Для эталонного ответа требуется {exercise.ReferenceDbType}, но такое подключение сейчас недоступно для этой базы данных"
                };
            }

            var userConnectionString = _databaseDeploymentService.BuildConnectionString(
                userDeployment.DbMeta!,
                userDeployment.DatabaseMeta!.PhysicalName);
            var referenceConnectionString = _databaseDeploymentService.BuildConnectionString(
                referenceDeployment.DbMeta!,
                referenceDeployment.DatabaseMeta!.PhysicalName);

            try
            {
                var userResult = await ExecuteQueryAsync(userDeployment.DbMeta!.Provider!, userConnectionString, dto.UserQuery);
                var referenceResult = await ExecuteQueryAsync(referenceDeployment.DbMeta!.Provider!, referenceConnectionString, exercise.CorrectAnswer);
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

        private async Task<DatabaseDeployment?> ResolveReferenceDeploymentAsync(Exercise exercise, DatabaseDeployment userDeployment)
        {
            if (string.IsNullOrWhiteSpace(exercise.ReferenceDbType) ||
                string.Equals(exercise.ReferenceDbType, userDeployment.DbMeta?.dbType, StringComparison.OrdinalIgnoreCase))
            {
                return userDeployment;
            }

            return await _context.DatabaseDeployments
                .Include(d => d.DbMeta)
                .Include(d => d.DatabaseMeta)
                .Where(d => d.DatabaseMetaId == exercise.DatabaseMetaId)
                .FirstOrDefaultAsync(d => d.DbMeta != null && d.DbMeta.dbType == exercise.ReferenceDbType);
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

            var normalizedColumns1 = GetNormalizedColumnNames(dt1);
            var normalizedColumns2 = GetNormalizedColumnNames(dt2);
            if (!normalizedColumns1.SequenceEqual(normalizedColumns2))
                return false;

            var sorted1 = GetNormalizedSortedRows(dt1);
            var sorted2 = GetNormalizedSortedRows(dt2);

            for (var i = 0; i < sorted1.Count; i++)
            {
                if (!sorted1[i].SequenceEqual(sorted2[i]))
                    return false;
            }

            return true;
        }

        private List<string[]> GetNormalizedSortedRows(DataTable table)
        {
            return table.AsEnumerable()
                .Select(row => row.ItemArray.Select(NormalizeValueForComparison).ToArray())
                .OrderBy(row => string.Join("|", row))
                .ToList();
        }

        private List<string> GetNormalizedColumnNames(DataTable table)
        {
            return table.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName.Trim().ToLowerInvariant())
                .ToList();
        }

        private string NormalizeValueForComparison(object? value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is bool boolValue)
                return boolValue ? "true" : "false";

            if (value is DateTime dateTime)
            {
                var normalized = dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime.ToUniversalTime();

                return normalized.TimeOfDay == TimeSpan.Zero
                    ? normalized.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : normalized.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            }

            if (value is DateOnly dateOnly)
                return dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (value is TimeOnly timeOnly)
                return timeOnly.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
            {
                return parsedDate.TimeOfDay == TimeSpan.Zero
                    ? parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : parsedDate.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            }

            return value.ToString()?.Trim() ?? string.Empty;
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
                return $"Количество столбцов не совпадает: у вас {user.Columns.Count}, ожидается {reference.Columns.Count}.";

            var normalizedUserColumns = GetNormalizedColumnNames(user);
            var normalizedReferenceColumns = GetNormalizedColumnNames(reference);
            if (!normalizedUserColumns.SequenceEqual(normalizedReferenceColumns))
            {
                var columnDifference = BuildColumnDifferenceMessage(user, reference, normalizedUserColumns, normalizedReferenceColumns);
                return $"Имена или порядок столбцов не совпадают. {columnDifference}";
            }

            var sortedUser = GetNormalizedSortedRows(user);
            var sortedReference = GetNormalizedSortedRows(reference);

            if (user.Rows.Count != reference.Rows.Count)
            {
                var extraUserRows = sortedUser.Except(sortedReference, StringArrayComparer.Instance).ToList();
                var missingUserRows = sortedReference.Except(sortedUser, StringArrayComparer.Instance).ToList();

                if (extraUserRows.Count > 0 && missingUserRows.Count > 0)
                {
                    return $"Количество строк не совпадает: у вас {user.Rows.Count}, ожидается {reference.Rows.Count}. Есть лишние строки, например: {FormatRowPreview(user, extraUserRows[0])}. И есть отсутствующие строки, например: {FormatRowPreview(reference, missingUserRows[0])}.";
                }

                if (extraUserRows.Count > 0)
                {
                    return $"Количество строк не совпадает: у вас {user.Rows.Count}, ожидается {reference.Rows.Count}. Есть лишняя строка, например: {FormatRowPreview(user, extraUserRows[0])}.";
                }

                if (missingUserRows.Count > 0)
                {
                    return $"Количество строк не совпадает: у вас {user.Rows.Count}, ожидается {reference.Rows.Count}. Не хватает строки, например: {FormatRowPreview(reference, missingUserRows[0])}.";
                }

                return $"Количество строк не совпадает: у вас {user.Rows.Count}, ожидается {reference.Rows.Count}.";
            }

            var originalColumnNames = user.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            for (var rowIndex = 0; rowIndex < sortedUser.Count; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < sortedUser[rowIndex].Length; columnIndex++)
                {
                    if (sortedUser[rowIndex][columnIndex] == sortedReference[rowIndex][columnIndex])
                        continue;

                    var columnName = columnIndex < originalColumnNames.Count ? originalColumnNames[columnIndex] : $"#{columnIndex + 1}";
                    return $"Данные не совпадают в строке {rowIndex + 1}, столбце '{columnName}': у вас '{sortedUser[rowIndex][columnIndex]}', ожидается '{sortedReference[rowIndex][columnIndex]}'.";
                }
            }

            return "Результат запроса отличается от ожидаемого. Проверьте выбранные столбцы, условия и формат данных.";
        }

        private string BuildColumnDifferenceMessage(
            DataTable user,
            DataTable reference,
            IReadOnlyList<string> normalizedUserColumns,
            IReadOnlyList<string> normalizedReferenceColumns)
        {
            var minCount = Math.Min(normalizedUserColumns.Count, normalizedReferenceColumns.Count);
            for (var columnIndex = 0; columnIndex < minCount; columnIndex++)
            {
                if (normalizedUserColumns[columnIndex] == normalizedReferenceColumns[columnIndex])
                    continue;

                var userColumnName = user.Columns[columnIndex].ColumnName;
                var referenceColumnName = reference.Columns[columnIndex].ColumnName;
                return $"На позиции {columnIndex + 1} у вас '{userColumnName}', ожидается '{referenceColumnName}'.";
            }

            return $"У вас: {string.Join(", ", user.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}. Ожидается: {string.Join(", ", reference.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}.";
        }

        private string FormatRowPreview(DataTable table, IReadOnlyList<string> normalizedRow)
        {
            var pairs = table.Columns.Cast<DataColumn>()
                .Select((column, index) => $"{column.ColumnName}={normalizedRow[index]}");

            return string.Join(", ", pairs);
        }

        private sealed class StringArrayComparer : IEqualityComparer<string[]>
        {
            public static StringArrayComparer Instance { get; } = new();

            public bool Equals(string[]? x, string[]? y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x is null || y is null || x.Length != y.Length)
                    return false;

                for (var i = 0; i < x.Length; i++)
                {
                    if (!string.Equals(x[i], y[i], StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            public int GetHashCode(string[] obj)
            {
                var hash = new HashCode();
                foreach (var item in obj)
                    hash.Add(item, StringComparer.Ordinal);

                return hash.ToHashCode();
            }
        }

        private List<string> GetColumnNames(DataTable dt)
        {
            return dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
        }

        private List<List<string>> DataTableToList(DataTable dt)
        {
            return dt.AsEnumerable()
                .Select(row => row.ItemArray.Select(value => value?.ToString() ?? "NULL").ToList())
                .ToList();
        }
    }
}

