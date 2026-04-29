using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Models;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<PagedResult<UserDto>> GetAllWithRolesAsync(int page, int pageSize, string? search = null, string? role = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var hasRole = !string.IsNullOrWhiteSpace(role);

            var conditions = new List<string>();

            if (hasSearch)
                conditions.Add(@"(u.FirstName LIKE @Search
                    OR u.LastName LIKE @Search
                    OR u.Email LIKE @Search
                    OR u.DocNumber LIKE @Search
                    OR u.PhoneNumber LIKE @Search)");

            if (hasRole)
                conditions.Add("r.Name = @Role");

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            var countSql = $@"
                SELECT COUNT(*)
                FROM Users u
                LEFT JOIN UserRoles ur ON u.Id = ur.UserId
                LEFT JOIN Roles r ON ur.RoleId = r.Id
                {whereClause}";

            using var countCommand = new SqlCommand(countSql, connection);
            if (hasSearch)
                countCommand.Parameters.AddWithValue("@Search", $"%{search}%");
            if (hasRole)
                countCommand.Parameters.AddWithValue("@Role", role);

            var totalCount = (int)(await countCommand.ExecuteScalarAsync() ?? 0);

            var sql = $@"
                SELECT u.Id AS UserId, u.FirstName, u.LastName, u.Email, u.DocNumber, u.PhoneNumber,
                       ISNULL(r.Name, '') AS Role
                FROM Users u
                LEFT JOIN UserRoles ur ON u.Id = ur.UserId
                LEFT JOIN Roles r ON ur.RoleId = r.Id
                {whereClause}
                ORDER BY u.FirstName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            if (hasSearch)
                command.Parameters.AddWithValue("@Search", $"%{search}%");
            if (hasRole)
                command.Parameters.AddWithValue("@Role", role);
            using var reader = await command.ExecuteReaderAsync();

            var users = new List<UserDto>();
            while (await reader.ReadAsync())
            {
                users.Add(new UserDto
                {
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = await reader.IsDBNullAsync(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                    DocNumber = reader.GetString(reader.GetOrdinal("DocNumber")),
                    PhoneNumber = await reader.IsDBNullAsync(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    Role = reader.GetString(reader.GetOrdinal("Role"))
                });
            }

            return new PagedResult<UserDto>
            {
                Items = users,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
