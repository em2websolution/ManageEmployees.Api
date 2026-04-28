using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<RefreshToken?> GetByUserIdAsync(string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT Id, UserId, Token, ExpireDate FROM RefreshTokens WHERE UserId = @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new RefreshToken
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    Token = reader.GetString(reader.GetOrdinal("Token")),
                    ExpireDate = reader.GetDateTime(reader.GetOrdinal("ExpireDate"))
                };
            }
            return null;
        }

        public async Task CreateAsync(RefreshToken token)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = "INSERT INTO RefreshTokens (Id, UserId, Token, ExpireDate) VALUES (@Id, @UserId, @Token, @ExpireDate)";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", token.Id);
            command.Parameters.AddWithValue("@UserId", token.UserId);
            command.Parameters.AddWithValue("@Token", token.Token);
            command.Parameters.AddWithValue("@ExpireDate", token.ExpireDate);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("DELETE FROM RefreshTokens WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
