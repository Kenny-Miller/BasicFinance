using BasicFinance.Domain.Internal;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Internal;

public class DateTimeRangeTests
{
    [Fact]
    public void Constructor_ValidRange_CreatesSuccessfully()
    {
        // Arrange
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 12, 31);

        // Act
        var range = new DateTimeRange(start, end);

        // Assert
        Assert.Equal(start, range.RangeStartDate);
        Assert.Equal(end, range.RangeEndDate);
    }

    [Fact]
    public void Constructor_EqualDates_CreatesSuccessfully()
    {
        // Arrange
        var same = new DateTime(2026, 6, 15);

        // Act
        var range = new DateTimeRange(same, same);

        // Assert
        Assert.Equal(same, range.RangeStartDate);
        Assert.Equal(same, range.RangeEndDate);
    }

    [Fact]
    public void Constructor_StartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        var start = new DateTime(2026, 12, 31);
        var end = new DateTime(2026, 1, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DateTimeRange(start, end));
    }

    [Fact]
    public void Constructor_StartAfterEnd_MessageIndicatesConstraint()
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() => new DateTimeRange(new DateTime(2026, 12, 31), new DateTime(2026, 1, 1)));

        // Assert
        Assert.Contains("RangeStartDate", ex.Message);
    }
}
