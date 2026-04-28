using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ManageEmployees.Infra.Data.Connection
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DBConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DBConnection' not found.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
