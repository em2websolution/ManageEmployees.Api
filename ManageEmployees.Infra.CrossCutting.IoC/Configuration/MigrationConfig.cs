using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ManageEmployees.Infra.Data.Contexts;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration
{
    public static class MigrationConfig
    {
        public static void RunMigrations(this WebApplication app)
        {
            using var serviceScope = app.Services.CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<LoginContext>();
            context?.Database.Migrate();
        }
    }
}
