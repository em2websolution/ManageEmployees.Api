using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.CrossCutting.IoC.Configuration;
using ManageEmployees.Services.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class IdentityConfigTests
{
    private ServiceCollection _services = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "JwtBearerTokenSettings:SecretKey", "ThisIsASecretKeyThatIsLongEnough1234567890!!" },
            { "JwtBearerTokenSettings:Audience", "test-audience" },
            { "JwtBearerTokenSettings:Issuer", "test-issuer" },
            { "JwtBearerTokenSettings:ExpiresAt", "7" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Test]
    public void AddIdentityConfiguration_ShouldRegisterJwtSettings_AsSingleton()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        var provider = _services.BuildServiceProvider();
        var settings = provider.GetRequiredService<JwtSettings>();

        settings.SecretKey.Should().Be("ThisIsASecretKeyThatIsLongEnough1234567890!!");
        settings.Audience.Should().Be("test-audience");
        settings.Issuer.Should().Be("test-issuer");
        settings.ExpiresAt.Should().Be("7");
    }

    [Test]
    public void AddIdentityConfiguration_ShouldRegisterIdentityCore()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        _services.Any(s => s.ServiceType == typeof(UserManager<User>)).Should().BeTrue();
        _services.Any(s => s.ServiceType == typeof(IUserStore<User>)).Should().BeTrue();
        _services.Any(s => s.ServiceType == typeof(IRoleStore<IdentityRole>)).Should().BeTrue();
    }

    [Test]
    public void AddJwtSecurity_ShouldConfigureJwtBearerOptions()
    {
        _services.AddLogging();
        _services.AddJwtSecurity(_configuration);

        var provider = _services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        options.RequireHttpsMetadata.Should().BeTrue();
        options.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        options.TokenValidationParameters.ValidAudience.Should().Be("test-audience");
        options.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().Be("test-issuer");
        options.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        options.TokenValidationParameters.RequireExpirationTime.Should().BeTrue();
        options.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void AddJwtSecurity_ShouldConfigureDefaultAuthenticationScheme()
    {
        _services.AddLogging();
        _services.AddJwtSecurity(_configuration);

        var provider = _services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        authOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        authOptions.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Test]
    public async Task JwtBearerEvents_OnMessageReceived_ShouldReadFromCookie()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        var provider = _services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", "access_token=test-jwt-token");

        var messageContext = new MessageReceivedContext(httpContext, new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)), options);

        await options.Events.OnMessageReceived(messageContext);

        messageContext.Token.Should().Be("test-jwt-token");
    }

    [Test]
    public void AddIdentityConfiguration_ShouldThrow_WhenJwtSettingsNull()
    {
        _services.AddLogging();
        var emptyConfig = new ConfigurationBuilder().Build();

        var act = () => _services.AddIdentityConfiguration(emptyConfig);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void AddIdentityConfiguration_ShouldConfigurePasswordOptions()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        var provider = _services.BuildServiceProvider();
        var identityOptions = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        identityOptions.Password.RequireNonAlphanumeric.Should().BeFalse();
        identityOptions.Password.RequireDigit.Should().BeFalse();
        identityOptions.Password.RequireUppercase.Should().BeFalse();
        identityOptions.Password.RequireLowercase.Should().BeFalse();
        identityOptions.Password.RequiredLength.Should().Be(3);
        identityOptions.Lockout.MaxFailedAccessAttempts.Should().Be(5);
    }
}
