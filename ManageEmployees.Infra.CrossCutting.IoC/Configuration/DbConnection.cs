using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ManageEmployees.Infra.Data.Contexts;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration
{
    public static class DbConnection
    {
        public static IServiceCollection AddDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString("DBConnection");

            services.AddDbContext<LoginContext>(options => options.UseSqlServer(connection,
                providerOptions =>
                {
                    providerOptions.CommandTimeout(180);
                    providerOptions.UseRowNumberForPaging();
                    providerOptions.MigrationsAssembly("ManageEmployees.Api");
                }));

            return services;
        }
    }
}
