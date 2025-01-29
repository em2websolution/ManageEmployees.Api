using ManageEmployees.Domain;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class ConstantsTests
{
    [Test]
    public void TableName_Roles_ShouldBeCorrect()
    {
        // Assert
        Assert.That(TableName.Roles, Is.EqualTo("Roles"));
    }

    [Test]
    public void TableColumn_ShouldContainExpectedValues()
    {
        // Assert
        Assert.That(TableColumn.Id, Is.EqualTo("Id"));
        Assert.That(TableColumn.Name, Is.EqualTo("Name"));
        Assert.That(TableColumn.Segmento, Is.EqualTo("Segmento"));
        Assert.That(TableColumn.NormalizedName, Is.EqualTo("NormalizedName"));
    }

    [Test]
    public void RoleName_ShouldContainExpectedValues()
    {
        // Assert
        Assert.That(RoleName.Administrator, Is.EqualTo("Administrator"));
        Assert.That(RoleName.Director, Is.EqualTo("Director"));
        Assert.That(RoleName.Leader, Is.EqualTo("Leader"));
        Assert.That(RoleName.Employee, Is.EqualTo("Employee"));
    }

    [Test]
    public void Auth_ShouldContainExpectedValues()
    {
        // Assert
        Assert.That(Auth.DecryptKey, Is.EqualTo("@my-secret-key-@"));
        Assert.That(Auth.DecriptStringError, Is.EqualTo("Formato de senha criptografada inválido."));
        Assert.That(Auth.DecriptKeyError, Is.EqualTo("The decryption key must be exactly 16 bytes long."));
    }
}
