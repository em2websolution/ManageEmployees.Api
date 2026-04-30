using FluentAssertions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Infra.CrossCutting.IoC.Configuration;
using ManageEmployees.Infra.Data.Connection;
using ManageEmployees.Infra.Data.Repositories;
using ManageEmployees.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class DependencyInjectionTests
{
    private ServiceCollection _services;

    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        _services.AddDependencyInjection(configMock.Object);
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterDbConnectionFactory()
    {
        _services.Any(s => s.ServiceType == typeof(IDbConnectionFactory)
            && s.ImplementationType == typeof(SqlConnectionFactory))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterRefreshTokenRepository()
    {
        _services.Any(s => s.ServiceType == typeof(IRefreshTokenRepository)
            && s.ImplementationType == typeof(RefreshTokenRepository))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterTaskRepository()
    {
        _services.Any(s => s.ServiceType == typeof(ITaskRepository)
            && s.ImplementationType == typeof(TaskRepository))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterUserRepository()
    {
        _services.Any(s => s.ServiceType == typeof(IUserRepository)
            && s.ImplementationType == typeof(UserRepository))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterAuthService()
    {
        _services.Any(s => s.ServiceType == typeof(IAuthService)
            && s.ImplementationType == typeof(AuthService))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterUserQueryService()
    {
        _services.Any(s => s.ServiceType == typeof(IUserQueryService)
            && s.ImplementationType == typeof(UserService))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterUserCommandService()
    {
        _services.Any(s => s.ServiceType == typeof(IUserCommandService)
            && s.ImplementationType == typeof(UserService))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterTaskQueryService()
    {
        _services.Any(s => s.ServiceType == typeof(ITaskQueryService)
            && s.ImplementationType == typeof(TaskService))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldRegisterTaskCommandService()
    {
        _services.Any(s => s.ServiceType == typeof(ITaskCommandService)
            && s.ImplementationType == typeof(TaskService))
            .Should().BeTrue();
    }

    [Test]
    public void AddDependencyInjection_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();

        // Act
        var result = services.AddDependencyInjection(configMock.Object);

        // Assert
        result.Should().BeSameAs(services);
    }
}
