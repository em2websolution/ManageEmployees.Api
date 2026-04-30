using FluentAssertions;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class PagedResultTests
{
    [Test]
    public void TotalPages_ShouldCalculateCorrectly_WhenNotExactDivision()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            TotalCount = 25,
            PageSize = 10
        };

        // Act & Assert
        result.TotalPages.Should().Be(3);
    }

    [Test]
    public void TotalPages_ShouldCalculateCorrectly_WhenExactDivision()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            TotalCount = 20,
            PageSize = 10
        };

        // Act & Assert
        result.TotalPages.Should().Be(2);
    }

    [Test]
    public void TotalPages_ShouldBeZero_WhenNoItems()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            TotalCount = 0,
            PageSize = 10
        };

        // Act & Assert
        result.TotalPages.Should().Be(0);
    }

    [Test]
    public void TotalPages_ShouldBeOne_WhenSingleItem()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            TotalCount = 1,
            PageSize = 10
        };

        // Act & Assert
        result.TotalPages.Should().Be(1);
    }

    [Test]
    public void Items_ShouldDefaultToEmptyList()
    {
        // Arrange & Act
        var result = new PagedResult<int>();

        // Assert
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Test]
    public void Properties_ShouldBeSetCorrectly()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var result = new PagedResult<string>
        {
            Items = items,
            Page = 2,
            PageSize = 10,
            TotalCount = 23
        };

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(23);
        result.TotalPages.Should().Be(3);
    }
}
