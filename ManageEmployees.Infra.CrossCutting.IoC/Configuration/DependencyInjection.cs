using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Infra.Data.Connection;
using ManageEmployees.Infra.Data.Repositories;
using ManageEmployees.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            AddConnectionFactory(services);
            AddRepositoriesDependencyInjection(services);
            AddServicesDependencyInjection(services);

            return services;
        }

        private static void AddConnectionFactory(IServiceCollection services)
        {
            services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        }

        private static void AddRepositoriesDependencyInjection(IServiceCollection services)
        {
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
        }

        private static void AddServicesDependencyInjection(IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserQueryService, UserService>();
            services.AddScoped<IUserCommandService, UserService>();
            services.AddScoped<ITaskQueryService, TaskService>();
            services.AddScoped<ITaskCommandService, TaskService>();
        }
    }
}
