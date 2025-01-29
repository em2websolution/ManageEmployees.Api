using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.Contexts;
using ManageEmployees.Services.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration
{
    public static class IdentityConfig
    {
        private const string JWT_CONFIG = "JwtBearerTokenSettings";

        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            TokenSettings(services, configuration);

            services.AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredLength = 3;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
                .AddEntityFrameworkStores<LoginContext>()
                .AddDefaultTokenProviders();

            services.AddJwtSecurity(configuration);
            services.AddScoped<JwtSecurityExtensionEvents>();

            return services;
        }

        private static void TokenSettings(IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JWT_CONFIG).Get<JwtSettings>() ?? throw new ArgumentNullException(nameof(configuration), "JWT settings cannot be null");
            services.AddSingleton(jwtSettings);
        }

        public static IServiceCollection AddJwtSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(x => { x.Cookie.Name = "token"; })
            .AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = true;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtBearerTokenSettings:Audience"],

                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtBearerTokenSettings:Issuer"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtBearerTokenSettings:SecretKey"] ?? throw new ArgumentNullException(nameof(configuration), "JwtBearerTokenSettings:SecretKey is null"))),

                    ValidateLifetime = true,
                    RequireExpirationTime = true,

                    ClockSkew = TimeSpan.Zero
                };
                opt.EventsType = typeof(JwtSecurityExtensionEvents);
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["access_token"];
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy(
                    "Bearer", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme‌​)
                    .RequireAuthenticatedUser().Build());
            });

            return services;
        }
    }
}
