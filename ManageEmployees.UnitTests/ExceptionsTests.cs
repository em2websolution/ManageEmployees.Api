using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;

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
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.EqualTo(innerException));
        Assert.That(exception.TraceId, Is.EqualTo(activity.Id)); 

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
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.EqualTo(innerException));
        Assert.That(exception.TraceId, Is.Null); 
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
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.Errors, Is.EquivalentTo(errors)); 
        Assert.That(exception.TraceId, Is.EqualTo(activity.Id)); 

        activity.Stop();
    }

    [Test]
    public void ExceptionExtensions_ShouldReturnBadRequest_ForValidationException()
    {
        // Arrange
        var validationException = new ValidationException("Validation error");

        // Act
        var problemDetails = validationException.ToProblemDetails();

        // Assert
        Assert.That(problemDetails.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
        Assert.That(problemDetails.Title, Is.EqualTo("Validation error"));
    }

    [Test]
    public void ExceptionExtensions_ShouldReturnForbidden_ForAuthenticationException()
    {
        // Arrange
        var authException = new AuthenticationException("Authentication error");

        // Act
        var problemDetails = authException.ToProblemDetails();

        // Assert
        Assert.That(problemDetails.Status, Is.EqualTo((int)HttpStatusCode.Forbidden));
        Assert.That(problemDetails.Title, Is.EqualTo("Authentication error"));
    }

    [Test]
    public void ExceptionExtensions_ShouldReturnNotImplemented_ForNotImplementedException()
    {
        // Arrange
        var notImplementedException = new NotImplementedException("Not implemented");

        // Act
        var problemDetails = notImplementedException.ToProblemDetails();

        // Assert
        Assert.That(problemDetails.Status, Is.EqualTo((int)HttpStatusCode.NotImplemented));
        Assert.That(problemDetails.Title, Is.EqualTo("Not implemented"));
    }

    [Test]
    public void ExceptionExtensions_ShouldReturnInternalServerError_ForUnknownException()
    {
        // Arrange
        var unknownException = new Exception("Unknown error");

        // Act
        var problemDetails = unknownException.ToProblemDetails();

        // Assert
        Assert.That(problemDetails.Status, Is.EqualTo((int)HttpStatusCode.InternalServerError));
        Assert.That(problemDetails.Title, Is.EqualTo("Unknown error"));
    }
}
