using ManageEmployees.Domain;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Models;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Repositories
{
    public class TaskRepository(IDbConnectionFactory connectionFactory) : ITaskRepository
    {

        public async Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize, string? search = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var hasStatus = !string.IsNullOrWhiteSpace(status);
            var hasStartDate = startDate.HasValue;
            var hasEndDate = endDate.HasValue;

            var conditions = new List<string>();

            if (hasSearch)
                conditions.Add("(Title LIKE @Search OR Description LIKE @Search)");

            if (hasStatus)
                conditions.Add("Status = @Status");

            if (hasStartDate)
                conditions.Add("DueDate >= @StartDate");

            if (hasEndDate)
                conditions.Add("DueDate < @EndDate");

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            var countSql = $"SELECT COUNT(*) FROM Tasks {whereClause}";
            using var countCommand = new SqlCommand(countSql, connection);

            AddFilterParameters(countCommand, search, status, startDate, endDate);

            var totalCount = (int)(await countCommand.ExecuteScalarAsync() ?? 0);

            var orderBy = hasStartDate || hasEndDate ? "DueDate ASC" : "CreatedAt DESC";

            var sql = $@"
                SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt
                FROM Tasks
                {whereClause}
                ORDER BY {orderBy}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            AddFilterParameters(command, search, status, startDate, endDate);

            using var reader = await command.ExecuteReaderAsync();
            var tasks = new List<TaskItem>();
            while (await reader.ReadAsync())
            {
                tasks.Add(MapTask(reader));
            }

            return new PagedResult<TaskItem>
            {
                Items = tasks,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM Tasks WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapTask(reader) : null;
        }

        public async Task<List<TaskItem>> GetByUserIdAsync(string userId)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM Tasks WHERE UserId = @UserId ORDER BY CreatedAt DESC", connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            var tasks = new List<TaskItem>();
            while (await reader.ReadAsync())
            {
                tasks.Add(MapTask(reader));
            }
            return tasks;
        }

        public async Task CreateAsync(TaskItem task)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO Tasks (Id, Title, Description, Status, DueDate, UserId, CreatedAt)
                VALUES (@Id, @Title, @Description, @Status, @DueDate, @UserId, @CreatedAt)";

            using var command = new SqlCommand(sql, connection);
            AddTaskParameters(command, task);
            command.Parameters.AddWithValue("@UserId", task.UserId);
            command.Parameters.AddWithValue("@CreatedAt", task.CreatedAt);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(TaskItem task)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE Tasks SET
                    Title = @Title, Description = @Description, Status = @Status,
                    DueDate = @DueDate
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            AddTaskParameters(command, task);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("DELETE FROM Tasks WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static void AddTaskParameters(SqlCommand command, TaskItem task)
        {
            command.Parameters.AddWithValue("@Id", task.Id);
            command.Parameters.AddWithValue("@Title", task.Title);
            command.Parameters.AddWithValue("@Description", (object?)task.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", task.Status);
            command.Parameters.AddWithValue("@DueDate", task.DueDate);
        }

        private static void AddFilterParameters(SqlCommand command, string? search, string? status, DateTime? startDate, DateTime? endDate)
        {
            if (!string.IsNullOrWhiteSpace(search))
                command.Parameters.AddWithValue("@Search", $"%{search}%");
            if (!string.IsNullOrWhiteSpace(status))
                command.Parameters.AddWithValue("@Status", status);
            if (startDate.HasValue)
                command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
            if (endDate.HasValue)
                command.Parameters.AddWithValue("@EndDate", endDate.Value.Date.AddDays(1));
        }

        private static TaskItem MapTask(SqlDataReader reader) => new()
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
