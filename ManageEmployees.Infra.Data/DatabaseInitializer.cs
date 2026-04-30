using ManageEmployees.Domain;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ManageEmployees.Infra.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

            try
            {
                await EnsureDatabaseExistsAsync(scope.ServiceProvider);
                await ExecuteSchemaScriptsAsync(scope.ServiceProvider);
                await SeedRolesAsync(scope.ServiceProvider);
                await SeedAdminUserAsync(scope.ServiceProvider);
                await SeedSampleTasksAsync(scope.ServiceProvider);

                logger.LogInformation("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        private static async Task EnsureDatabaseExistsAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DBConnection")!;
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @dbName) BEGIN DECLARE @sql NVARCHAR(500) = N'CREATE DATABASE ' + QUOTENAME(@dbName); EXEC sp_executesql @sql; END";
            command.Parameters.AddWithValue("@dbName", databaseName);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task ExecuteSchemaScriptsAsync(IServiceProvider services)
        {
            var connectionFactory = services.GetRequiredService<IDbConnectionFactory>();
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var script = GetEmbeddedScript("001_CreateTables.sql");

            var batches = script.Split(
                ["\nGO\n", "\nGO\r\n", "\r\nGO\r\n", "\r\nGO\n"],
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                using var command = connection.CreateCommand();
                command.CommandText = trimmed;
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task SeedRolesAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = [RoleName.Administrator, RoleName.Employee];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUserAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<User>>();

            if (await userManager.FindByNameAsync("admin@company.com") is not null)
                return;

            var admin = new User
            {
                UserName = "admin@company.com",
                Email = "admin@company.com",
                FirstName = "Admin",
                LastName = "User",
                DocNumber = "00000000000",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, RoleName.Administrator);
            }
        }

        private static async Task SeedSampleTasksAsync(IServiceProvider services)
        {
            var taskRepo = services.GetRequiredService<ITaskRepository>();
            var tasks = await taskRepo.GetAllAsync(1, 1);

            if (tasks.TotalCount > 0)
                return;

            var userManager = services.GetRequiredService<UserManager<User>>();
            var admin = await userManager.FindByNameAsync("admin@company.com");
            if (admin is null) return;

            await taskRepo.CreateAsync(new TaskItem
            {
                Title = "Review project architecture",
                Description = "Review and document the clean architecture implementation",
                Status = TaskItemStatus.InProgress,
                DueDate = DateTime.UtcNow.AddDays(7),
                UserId = admin.Id
            });

            await taskRepo.CreateAsync(new TaskItem
            {
                Title = "Write unit tests",
                Description = "Add comprehensive unit tests for all layers",
                Status = TaskItemStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(14),
                UserId = admin.Id
            });

            await taskRepo.CreateAsync(new TaskItem
            {
                Title = "Setup CI/CD pipeline",
                Description = "Configure continuous integration and deployment",
                Status = TaskItemStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(21),
                UserId = admin.Id
            });
        }

        private static string GetEmbeddedScript(string scriptName)
        {
            var assembly = Assembly.GetAssembly(typeof(DatabaseInitializer))!;
            var resourceName = $"ManageEmployees.Infra.Data.Scripts.{scriptName}";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
