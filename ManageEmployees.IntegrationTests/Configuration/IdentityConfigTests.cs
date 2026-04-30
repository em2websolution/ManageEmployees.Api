using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.CrossCutting.IoC.Configuration;
using ManageEmployees.Services.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManageEmployees.IntegrationTests.Configuration;

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
            { "JwtBearerTokenSettings:SecretKey", "ThisIsASecretKeyThatIsLongEnough1234567890" },
            { "JwtBearerTokenSettings:Audience", "test-audience" },
            { "JwtBearerTokenSettings:Issuer", "test-issuer" },
            { "JwtBearerTokenSettings:ExpiryTimeInSeconds", "3600" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Test]
    public void AddIdentityConfiguration_ShouldRegisterUserManager()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        _services.Any(s => s.ServiceType == typeof(UserManager<User>)).Should().BeTrue();
    }

    [Test]
    public void AddIdentityConfiguration_ShouldRegisterJwtSettings()
    {
        _services.AddLogging();
        _services.AddIdentityConfiguration(_configuration);

        _services.Any(s => s.ServiceType == typeof(JwtSettings)).Should().BeTrue();
    }

    [Test]
    public void AddIdentityConfiguration_ShouldReturnServiceCollection()
    {
        _services.AddLogging();
        var result = _services.AddIdentityConfiguration(_configuration);

        result.Should().BeSameAs(_services);
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
    public void AddJwtSecurity_ShouldRegisterAuthenticationServices()
    {
        _services.AddLogging();
        _services.AddJwtSecurity(_configuration);

        _services.Any(s => s.ServiceType.Name.Contains("IAuthenticationService")).Should().BeTrue();
    }
}
