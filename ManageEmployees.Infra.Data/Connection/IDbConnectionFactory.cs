using Microsoft.Data.SqlClient;

namespace ManageEmployees.Infra.Data.Connection
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
