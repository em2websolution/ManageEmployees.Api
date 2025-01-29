using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration
{
    public static class ConfigurationDecryptor
    {
        public static void DecryptConfigurationValues(this IServiceCollection services, IConfiguration configuration)
        {
            var encryptionService = services.BuildServiceProvider().GetRequiredService<IEncryptionService>();

            var DBConnection = configuration["ConnectionStrings:DBConnection"];
            if (!string.IsNullOrEmpty(DBConnection))
            {
                configuration["ConnectionStrings:DBConnection"] = DecryptConnectionStringPassword(DBConnection, encryptionService);
            }

            DecryptAndLogIfEncrypted(configuration, "JwtBearerTokenSettings:SecretKey", encryptionService);
        }

        private static void DecryptAndLogIfEncrypted(IConfiguration configuration, string key, IEncryptionService encryptionService)
        {
            var encryptedValue = configuration[key];
            if (!string.IsNullOrEmpty(encryptedValue) && encryptedValue.Contains(':'))
            {
                var decryptedValue = encryptionService.Decrypt(encryptedValue);
                configuration[key] = decryptedValue;
            }
        }

        private static string DecryptConnectionStringPassword(string connectionString, IEncryptionService encryptionService)
        {
            var passwordStartIndex = connectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            if (passwordStartIndex < 0) return connectionString;

            var passwordEndIndex = connectionString.IndexOf(';', passwordStartIndex);
            var encryptedPassword = connectionString.Substring(
                passwordStartIndex + "Password=".Length,
                (passwordEndIndex > passwordStartIndex ? passwordEndIndex : connectionString.Length) - passwordStartIndex - "Password=".Length
            );

            if (!string.IsNullOrEmpty(encryptedPassword) && encryptedPassword.Contains(':'))
            {
                var decryptedPassword = encryptionService.Decrypt(encryptedPassword);
                var updatedConnectionString = connectionString.Replace(encryptedPassword, decryptedPassword);
                return updatedConnectionString;
            }

            return connectionString;
        }
    }
}