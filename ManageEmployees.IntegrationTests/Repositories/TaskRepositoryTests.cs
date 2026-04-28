using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.Repositories;
using ManageEmployees.IntegrationTests;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.IntegrationTests.Repositories;

[TestFixture]
public class TaskRepositoryTests
{
    private TaskRepository _repository = null!;
    private string _testUserId = null!;

    [SetUp]
    public async Task SetUp()
    {
        await DatabaseFixture.CleanTablesAsync();
        _repository = new TaskRepository(DatabaseFixture.ConnectionFactory);
        _testUserId = await SeedTestUserAsync();
    }

    [Test]
    public async Task CreateAsync_ShouldInsertTask()
    {
        var task = CreateTaskItem("Test Task");

        await _repository.CreateAsync(task);

        var result = await _repository.GetByIdAsync(task.Id);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.UserId.Should().Be(_testUserId);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllTasks()
    {
        await _repository.CreateAsync(CreateTaskItem("Task 1"));
        await _repository.CreateAsync(CreateTaskItem("Task 2"));
        await _repository.CreateAsync(CreateTaskItem("Task 3"));

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Test]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ShouldReturnTask()
    {
        var task = CreateTaskItem("Find Me");
        await _repository.CreateAsync(task);

        var result = await _repository.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be("Find Me");
        result.Status.Should().Be("Pending");
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserTasks()
    {
        var secondUserId = await SeedTestUserAsync("second@test.com");

        await _repository.CreateAsync(CreateTaskItem("User1 Task", _testUserId));
        await _repository.CreateAsync(CreateTaskItem("User2 Task", secondUserId));

        var result = await _repository.GetByUserIdAsync(_testUserId);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("User1 Task");
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyExistingTask()
    {
        var task = CreateTaskItem("Original Title");
        await _repository.CreateAsync(task);

        task.Title = "Updated Title";
        task.Status = "Completed";
        task.Description = "Updated description";
        await _repository.UpdateAsync(task);

        var result = await _repository.GetByIdAsync(task.Id);
        result!.Title.Should().Be("Updated Title");
        result.Status.Should().Be("Completed");
        result.Description.Should().Be("Updated description");
    }

    [Test]
    public async Task DeleteAsync_WhenExists_ShouldReturnTrueAndRemove()
    {
        var task = CreateTaskItem("Delete Me");
        await _repository.CreateAsync(task);

        var deleted = await _repository.DeleteAsync(task.Id);

        deleted.Should().BeTrue();
        var result = await _repository.GetByIdAsync(task.Id);
        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_ShouldReturnFalse()
    {
        var deleted = await _repository.DeleteAsync(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    [Test]
    public async Task CreateAsync_WithNullDescription_ShouldPersistNull()
    {
        var task = CreateTaskItem("No Description");
        task.Description = null;
        await _repository.CreateAsync(task);

        var result = await _repository.GetByIdAsync(task.Id);
        result!.Description.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnOrderedByCreatedAtDesc()
    {
        var task1 = CreateTaskItem("First");
        task1.CreatedAt = DateTime.UtcNow.AddHours(-2);
        await _repository.CreateAsync(task1);

        var task2 = CreateTaskItem("Second");
        task2.CreatedAt = DateTime.UtcNow.AddHours(-1);
        await _repository.CreateAsync(task2);

        var task3 = CreateTaskItem("Third");
        task3.CreatedAt = DateTime.UtcNow;
        await _repository.CreateAsync(task3);

        var result = await _repository.GetAllAsync();

        result[0].Title.Should().Be("Third");
        result[1].Title.Should().Be("Second");
        result[2].Title.Should().Be("First");
    }

    private TaskItem CreateTaskItem(string title, string? userId = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "Test description",
        Status = "Pending",
        DueDate = DateTime.UtcNow.AddDays(7),
        UserId = userId ?? _testUserId,
        CreatedAt = DateTime.UtcNow
    };

    private static async Task<string> SeedTestUserAsync(string email = "test@test.com")
    {
        var userId = Guid.NewGuid().ToString();
        using var connection = new SqlConnection(DatabaseFixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                               SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
                               LockoutEnabled, AccessFailedCount, FirstName, LastName, DocNumber)
            VALUES (@Id, @Email, @NormalizedEmail, @Email, @NormalizedEmail, 0,
                    @Stamp, @Stamp, 0, 0, 0, 0, 'Test', 'User', '12345678900')";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@NormalizedEmail", email.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
        await cmd.ExecuteNonQueryAsync();

        return userId;
    }
}
