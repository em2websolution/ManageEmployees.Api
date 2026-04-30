using ManageEmployees.Api.Middlewares;
using ManageEmployees.Infra.CrossCutting.IoC.Configuration;
using ManageEmployees.Infra.Data;
using ManageEmployees.Services.Settings;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

#region Log

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

#endregion

builder.Services.AddDependencyInjection(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddIdentityConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var apiVersion = builder.Configuration.GetValue<string>("ApiVersion");

builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc(apiVersion, new OpenApiInfo { Title = "ManageEmployees.Api", Version = apiVersion });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
                    "JWT Authorization Header - used with Bearer Authentication.\r\n\r\n" +
                    "Enter 'Bearer' [space] and then your token in the field below.\r\n\r\n" +
                    "Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    opt.IncludeXmlComments(xmlPath);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000"];
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
        });
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", "ManageEmployees.Api");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<LogSettings>();
app.Run();