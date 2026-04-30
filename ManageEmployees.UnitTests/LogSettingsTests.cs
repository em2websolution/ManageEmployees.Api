using FluentAssertions;
using ManageEmployees.Services.Settings;
using Microsoft.AspNetCore.Http;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class LogSettingsTests
{
    [Test]
    public async Task Invoke_ShouldCallNextDelegate()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logSettings = new LogSettings(next);
        var context = new DefaultHttpContext();

        // Act
        await logSettings.Invoke(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task Invoke_ShouldCompleteWithoutError()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logSettings = new LogSettings(next);
        var context = new DefaultHttpContext();

        // Act
        Func<Task> act = async () => await logSettings.Invoke(context);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
