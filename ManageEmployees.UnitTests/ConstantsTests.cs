using ManageEmployees.Domain;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class ConstantsTests
{
    [Test]
    public void RoleName_ShouldContainExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RoleName.Administrator, Is.EqualTo("Administrator"));
            Assert.That(RoleName.Employee, Is.EqualTo("Employee"));
        });
    }

    [Test]
    public void TaskItemStatus_ShouldContainExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TaskItemStatus.Pending, Is.EqualTo("Pending"));
            Assert.That(TaskItemStatus.InProgress, Is.EqualTo("InProgress"));
            Assert.That(TaskItemStatus.Completed, Is.EqualTo("Completed"));
            Assert.That(TaskItemStatus.All, Has.Length.EqualTo(3));
            Assert.That(TaskItemStatus.All, Contains.Item("Pending"));
            Assert.That(TaskItemStatus.All, Contains.Item("InProgress"));
            Assert.That(TaskItemStatus.All, Contains.Item("Completed"));
        });
    }
}
