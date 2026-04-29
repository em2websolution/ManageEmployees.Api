using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Identity
{
    public sealed class UserStore :
        IUserPasswordStore<User>,
        IUserRoleStore<User>,
        IUserSecurityStampStore<User>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserStore(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void Dispose()
        {
            // No unmanaged resources to release
            GC.SuppressFinalize(this);
        }

        #region IUserStore

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
                    TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                    FirstName, LastName, DocNumber)
                VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed,
                    @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed,
                    @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount,
                    @FirstName, @LastName, @DocNumber)";

            using var command = new SqlCommand(sql, connection);
            AddUserParameters(command, user);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE Users SET
                    UserName = @UserName, NormalizedUserName = @NormalizedUserName,
                    Email = @Email, NormalizedEmail = @NormalizedEmail, EmailConfirmed = @EmailConfirmed,
                    PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp, ConcurrencyStamp = @ConcurrencyStamp,
                    PhoneNumber = @PhoneNumber, PhoneNumberConfirmed = @PhoneNumberConfirmed,
                    TwoFactorEnabled = @TwoFactorEnabled, LockoutEnd = @LockoutEnd,
                    LockoutEnabled = @LockoutEnabled, AccessFailedCount = @AccessFailedCount,
                    FirstName = @FirstName, LastName = @LastName, DocNumber = @DocNumber
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            AddUserParameters(command, user);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("DELETE FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", userId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
        }

        public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("SELECT * FROM Users WHERE NormalizedUserName = @NormalizedUserName", connection);
            command.Parameters.AddWithValue("@NormalizedUserName", normalizedUserName);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
        }

        #endregion

        #region IUserPasswordStore

        public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken) { user.PasswordHash = passwordHash; return Task.CompletedTask; }
        public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        #endregion

        #region IUserRoleStore

        public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var findCmd = new SqlCommand("SELECT Id FROM Roles WHERE NormalizedName = @NormalizedName", connection);
            findCmd.Parameters.AddWithValue("@NormalizedName", roleName.ToUpperInvariant());
            var roleId = await findCmd.ExecuteScalarAsync(cancellationToken) as string
                ?? throw new InvalidOperationException($"Role '{roleName}' not found.");

            using var insertCmd = new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId) INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                connection);
            insertCmd.Parameters.AddWithValue("@UserId", user.Id);
            insertCmd.Parameters.AddWithValue("@RoleId", roleId);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                DELETE ur FROM UserRoles ur
                INNER JOIN Roles r ON ur.RoleId = r.Id
                WHERE ur.UserId = @UserId AND r.NormalizedName = @NormalizedName";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", user.Id);
            command.Parameters.AddWithValue("@NormalizedName", roleName.ToUpperInvariant());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT r.Name FROM Roles r
                INNER JOIN UserRoles ur ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", user.Id);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var roles = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(0);
                if (!string.IsNullOrEmpty(name))
                    roles.Add(name);
            }
            return roles;
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            var roles = await GetRolesAsync(user, cancellationToken);
            return roles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT u.* FROM Users u
                INNER JOIN UserRoles ur ON u.Id = ur.UserId
                INNER JOIN Roles r ON ur.RoleId = r.Id
                WHERE r.NormalizedName = @NormalizedName";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@NormalizedName", roleName.ToUpperInvariant());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var users = new List<User>();
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(MapUser(reader));
            }
            return users;
        }

        #endregion

        #region IUserSecurityStampStore

        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken) { user.SecurityStamp = stamp; return Task.CompletedTask; }
        public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.SecurityStamp);

        #endregion

        #region Helpers

        private static void AddUserParameters(SqlCommand command, User user)
        {
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@UserName", (object?)user.UserName ?? DBNull.Value);
            command.Parameters.AddWithValue("@NormalizedUserName", (object?)user.NormalizedUserName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)user.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@NormalizedEmail", (object?)user.NormalizedEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmailConfirmed", user.EmailConfirmed);
            command.Parameters.AddWithValue("@PasswordHash", (object?)user.PasswordHash ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecurityStamp", (object?)user.SecurityStamp ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConcurrencyStamp", (object?)user.ConcurrencyStamp ?? DBNull.Value);
            command.Parameters.AddWithValue("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@PhoneNumberConfirmed", user.PhoneNumberConfirmed);
            command.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
            command.Parameters.AddWithValue("@LockoutEnd", (object?)user.LockoutEnd ?? DBNull.Value);
            command.Parameters.AddWithValue("@LockoutEnabled", user.LockoutEnabled);
            command.Parameters.AddWithValue("@AccessFailedCount", user.AccessFailedCount);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);
            command.Parameters.AddWithValue("@DocNumber", user.DocNumber);
        }

        private static User MapUser(SqlDataReader reader) => new()
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
            NormalizedUserName = reader.IsDBNull(reader.GetOrdinal("NormalizedUserName")) ? null : reader.GetString(reader.GetOrdinal("NormalizedUserName")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            NormalizedEmail = reader.IsDBNull(reader.GetOrdinal("NormalizedEmail")) ? null : reader.GetString(reader.GetOrdinal("NormalizedEmail")),
            EmailConfirmed = reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash")),
            SecurityStamp = reader.IsDBNull(reader.GetOrdinal("SecurityStamp")) ? null : reader.GetString(reader.GetOrdinal("SecurityStamp")),
            ConcurrencyStamp = reader.IsDBNull(reader.GetOrdinal("ConcurrencyStamp")) ? null : reader.GetString(reader.GetOrdinal("ConcurrencyStamp")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            PhoneNumberConfirmed = reader.GetBoolean(reader.GetOrdinal("PhoneNumberConfirmed")),
            TwoFactorEnabled = reader.GetBoolean(reader.GetOrdinal("TwoFactorEnabled")),
            LockoutEnd = reader.IsDBNull(reader.GetOrdinal("LockoutEnd")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("LockoutEnd")),
            LockoutEnabled = reader.GetBoolean(reader.GetOrdinal("LockoutEnabled")),
            AccessFailedCount = reader.GetInt32(reader.GetOrdinal("AccessFailedCount")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            DocNumber = reader.GetString(reader.GetOrdinal("DocNumber"))
        };

        #endregion
    }
}
