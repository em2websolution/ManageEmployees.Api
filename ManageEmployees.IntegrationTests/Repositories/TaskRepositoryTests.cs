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

        var result = await _repository.GetAllAsync(1, 10);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = await _repository.GetAllAsync(1, 10);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
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

        var result = await _repository.GetAllAsync(1, 10);

        result.Items[0].Title.Should().Be("Third");
        result.Items[1].Title.Should().Be("Second");
        result.Items[2].Title.Should().Be("First");
    }

    [Test]
    public async Task GetAllAsync_WithSearchFilter_ShouldReturnMatchingTasks()
    {
        await _repository.CreateAsync(CreateTaskItem("Important Report"));
        await _repository.CreateAsync(CreateTaskItem("Daily Standup"));
        await _repository.CreateAsync(CreateTaskItem("Important Review"));

        var result = await _repository.GetAllAsync(1, 10, search: "Important");

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(t => t.Title.Should().Contain("Important"));
    }

    [Test]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnMatchingTasks()
    {
        var pending = CreateTaskItem("Pending Task");
        pending.Status = "Pending";
        await _repository.CreateAsync(pending);

        var completed = CreateTaskItem("Completed Task");
        completed.Status = "Completed";
        await _repository.CreateAsync(completed);

        var inProgress = CreateTaskItem("InProgress Task");
        inProgress.Status = "InProgress";
        await _repository.CreateAsync(inProgress);

        var result = await _repository.GetAllAsync(1, 10, status: "Completed");

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Completed Task");
    }

    [Test]
    public async Task GetAllAsync_WithStartDateFilter_ShouldReturnTasksAfterDate()
    {
        var oldTask = CreateTaskItem("Old Task");
        oldTask.DueDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(oldTask);

        var recentTask = CreateTaskItem("Recent Task");
        recentTask.DueDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(recentTask);

        var futureTask = CreateTaskItem("Future Task");
        futureTask.DueDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(futureTask);

        var startDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _repository.GetAllAsync(1, 10, startDate: startDate);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.DueDate.Should().BeOnOrAfter(startDate));
    }

    [Test]
    public async Task GetAllAsync_WithEndDateFilter_ShouldReturnTasksBeforeDate()
    {
        var earlyTask = CreateTaskItem("Early Task");
        earlyTask.DueDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(earlyTask);

        var midTask = CreateTaskItem("Mid Task");
        midTask.DueDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(midTask);

        var lateTask = CreateTaskItem("Late Task");
        lateTask.DueDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(lateTask);

        var endDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = await _repository.GetAllAsync(1, 10, endDate: endDate);

        result.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAllAsync_WithStartAndEndDate_ShouldReturnTasksInRange_OrderedByDueDate()
    {
        var task1 = CreateTaskItem("Task Jan");
        task1.DueDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(task1);

        var task2 = CreateTaskItem("Task Mar");
        task2.DueDate = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(task2);

        var task3 = CreateTaskItem("Task May");
        task3.DueDate = new DateTime(2024, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(task3);

        var task4 = CreateTaskItem("Task Sep");
        task4.DueDate = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(task4);

        var startDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await _repository.GetAllAsync(1, 10, startDate: startDate, endDate: endDate);

        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Task Mar");
        result.Items[1].Title.Should().Be("Task May");
    }

    [Test]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var task = CreateTaskItem($"Task {i}");
            task.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            await _repository.CreateAsync(task);
        }

        var result = await _repository.GetAllAsync(2, 2);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
    }

    [Test]
    public async Task GetAllAsync_WithAllFilters_ShouldCombineConditions()
    {
        var match = CreateTaskItem("Deploy Feature");
        match.Status = "Pending";
        match.DueDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(match);

        var wrongStatus = CreateTaskItem("Deploy Hotfix");
        wrongStatus.Status = "Completed";
        wrongStatus.DueDate = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(wrongStatus);

        var wrongDate = CreateTaskItem("Deploy Old");
        wrongDate.Status = "Pending";
        wrongDate.DueDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(wrongDate);

        var wrongSearch = CreateTaskItem("Meeting");
        wrongSearch.Status = "Pending";
        wrongSearch.DueDate = new DateTime(2024, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        await _repository.CreateAsync(wrongSearch);

        var startDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await _repository.GetAllAsync(1, 10, search: "Deploy", status: "Pending", startDate: startDate, endDate: endDate);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Deploy Feature");
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
