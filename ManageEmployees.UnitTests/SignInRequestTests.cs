using ManageEmployees.Domain.DTO;
using System.ComponentModel.DataAnnotations;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class SignInRequestTests
{
    [Test]
    public void SignInRequest_ShouldBeValid_WhenAllFieldsAreProvided()
    {
        // Arrange
        var request = new SignInRequest
        {
            UserName = "validUser",
            Password = "validPassword123"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.That(validationResults, Is.Empty);
    }

    [Test]
    public void SignInRequest_ShouldBeInvalid_WhenUserNameIsMissing()
    {
        // Arrange
        var request = new SignInRequest
        {
            Password = "validPassword123"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults[0].ErrorMessage, Is.EqualTo("The UserName field is required."));
    }

    [Test]
    public void SignInRequest_ShouldBeInvalid_WhenPasswordIsMissing()
    {
        // Arrange
        var request = new SignInRequest
        {
            UserName = "validUser"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults[0].ErrorMessage, Is.EqualTo("The Password field is required."));
    }

    [Test]
    public void SignInRequest_ShouldBeInvalid_WhenPasswordIsTooShort()
    {
        // Arrange
        var request = new SignInRequest
        {
            UserName = "validUser",
            Password = "123" // Less than 6 characters
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults[0].ErrorMessage, Is.EqualTo("Please enter at least 6 characters!"));
    }

    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}