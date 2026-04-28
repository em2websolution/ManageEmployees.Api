using ManageEmployees.Infra.Data.Connection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Identity
{
    public class RoleStore : IRoleStore<IdentityRole>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RoleStore(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void Dispose() { }

        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.Id);
        public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.Name);
        public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken ct) { role.Name = roleName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.NormalizedName);
        public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken ct) { role.NormalizedName = normalizedName; return Task.CompletedTask; }

        public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken ct)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            const string sql = "INSERT INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp)";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", role.Id);
            command.Parameters.AddWithValue("@Name", (object?)role.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@NormalizedName", (object?)role.NormalizedName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConcurrencyStamp", (object?)role.ConcurrencyStamp ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(ct);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken ct)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            const string sql = "UPDATE Roles SET Name = @Name, NormalizedName = @NormalizedName, ConcurrencyStamp = @ConcurrencyStamp WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", role.Id);
            command.Parameters.AddWithValue("@Name", (object?)role.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@NormalizedName", (object?)role.NormalizedName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConcurrencyStamp", (object?)role.ConcurrencyStamp ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(ct);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken ct)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            using var command = new SqlCommand("DELETE FROM Roles WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", role.Id);
            await command.ExecuteNonQueryAsync(ct);

            return IdentityResult.Success;
        }

        public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken ct)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            using var command = new SqlCommand("SELECT * FROM Roles WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", roleId);

            using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? MapRole(reader) : null;
        }

        public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken ct)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            using var command = new SqlCommand("SELECT * FROM Roles WHERE NormalizedName = @NormalizedName", connection);
            command.Parameters.AddWithValue("@NormalizedName", normalizedRoleName);

            using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? MapRole(reader) : null;
        }

        private static IdentityRole MapRole(SqlDataReader reader) => new()
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
            NormalizedName = reader.IsDBNull(reader.GetOrdinal("NormalizedName")) ? null : reader.GetString(reader.GetOrdinal("NormalizedName")),
            ConcurrencyStamp = reader.IsDBNull(reader.GetOrdinal("ConcurrencyStamp")) ? null : reader.GetString(reader.GetOrdinal("ConcurrencyStamp"))
        };
    }
}
