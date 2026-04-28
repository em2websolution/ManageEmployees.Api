using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Repositories;
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

        public async Task<List<UserDto>> GetAllWithRolesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT u.Id AS UserId, u.FirstName, u.LastName, u.Email, u.DocNumber, u.PhoneNumber,
                       ISNULL(r.Name, '') AS Role
                FROM Users u
                LEFT JOIN UserRoles ur ON u.Id = ur.UserId
                LEFT JOIN Roles r ON ur.RoleId = r.Id
                ORDER BY u.FirstName";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var users = new List<UserDto>();
            while (await reader.ReadAsync())
            {
                users.Add(new UserDto
                {
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                    DocNumber = reader.GetString(reader.GetOrdinal("DocNumber")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    Role = reader.GetString(reader.GetOrdinal("Role"))
                });
            }

            return users;
        }
    }
}
