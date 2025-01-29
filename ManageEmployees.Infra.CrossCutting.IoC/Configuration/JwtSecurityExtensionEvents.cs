using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace ManageEmployees.Infra.CrossCutting.IoC.Configuration;
public sealed class JwtSecurityExtensionEvents : JwtBearerEvents
{
    private readonly ILogger<JwtSecurityExtensionEvents> _logger;

    public JwtSecurityExtensionEvents(ILogger<JwtSecurityExtensionEvents> logger)
    {
        _logger = logger;
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        _logger.LogError("Token invalido, expirado ou nao informado...");
        await base.Challenge(context);
    }
}