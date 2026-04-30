using FluentAssertions;
using ManageEmployees.Infra.CrossCutting.IoC.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class JwtSecurityExtensionEventsTests
{
    private Mock<ILogger<JwtSecurityExtensionEvents>> _loggerMock;
    private JwtSecurityExtensionEvents _events;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<JwtSecurityExtensionEvents>>();
        _events = new JwtSecurityExtensionEvents(_loggerMock.Object);
    }

    private static JwtBearerChallengeContext CreateChallengeContext()
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        var properties = new AuthenticationProperties();
        return new JwtBearerChallengeContext(httpContext, scheme, options, properties);
    }

    [Test]
    public async Task Challenge_ShouldLogErrorMessage()
    {
        // Arrange
        var context = CreateChallengeContext();

        // Act
        await _events.Challenge(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Token invalido, expirado ou nao informado...")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task Challenge_ShouldCompleteWithoutException()
    {
        // Arrange
        var context = CreateChallengeContext();

        // Act
        Func<Task> act = async () => await _events.Challenge(context);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
