using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Models;
using System.Diagnostics;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class ExceptionsTests
{
    [Test]
    public void BusinessException_ShouldSetMessageAndInnerException_WhenInitializedWithInnerException()
    {
        // Arrange
        var message = "An error occurred.";
        var innerException = new InvalidOperationException("Inner exception message.");
        var activity = new Activity("TestActivity");
        activity.Start(); 

        // Act
        var exception = new BusinessException(message, innerException);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.That(exception.InnerException, Is.EqualTo(innerException));
            Assert.That(exception.TraceId, Is.EqualTo(activity.Id));
        });

        activity.Stop();
    }

    [Test]
    public void BusinessException_ShouldSetTraceIdToNull_WhenNoActivityIsPresent()
    {
        // Arrange
        var message = "An error occurred.";
        var innerException = new InvalidOperationException("Inner exception message.");

        Activity.Current = null;

        // Act
        var exception = new BusinessException(message, innerException);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.That(exception.InnerException, Is.EqualTo(innerException));
            Assert.That(exception.TraceId, Is.Null);
        });
    }

    [Test]
    public void BusinessException_ShouldSetMessageAndErrors_WhenInitializedWithErrors()
    {
        // Arrange
        var message = "An error occurred.";
        var errors = new List<Error>
        {
            new Error { Code = "ERR001", Message = "First error message." },
            new Error { Code = "ERR002", Message = "Second error message." }
        };

        var activity = new Activity("TestActivity");
        activity.Start(); 

        // Act
        var exception = new BusinessException(message, errors);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.That(exception.Errors, Is.EquivalentTo(errors));
            Assert.That(exception.TraceId, Is.EqualTo(activity.Id));
        }); 

        activity.Stop();
    }

    [Test]
    public void NotFoundException_ShouldSetMessage()
    {
        // Act
        var exception = new NotFoundException("Resource not found");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Resource not found"));
            Assert.That(exception, Is.InstanceOf<BusinessException>());
        });
    }
}
