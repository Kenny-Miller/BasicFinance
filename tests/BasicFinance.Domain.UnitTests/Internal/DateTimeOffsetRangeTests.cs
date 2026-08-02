using BasicFinance.Domain.Internal;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Internal;

public class DateTimeOffsetRangeTests
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    [Fact]
    public void Constructor_ValidRange_CreatesSuccessfully()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, Offset);
        var end = new DateTimeOffset(2026, 12, 31, 23, 59, 59, Offset);

        // Act
        var range = new DateTimeOffsetRange(start, end);

        // Assert
        Assert.Equal(start, range.RangeStartDate);
        Assert.Equal(end, range.RangeEndDate);
    }

    [Fact]
    public void Constructor_EqualDates_CreatesSuccessfully()
    {
        // Arrange
        var same = new DateTimeOffset(2026, 6, 15, 12, 0, 0, Offset);

        // Act
        var range = new DateTimeOffsetRange(same, same);

        // Assert
        Assert.Equal(same, range.RangeStartDate);
        Assert.Equal(same, range.RangeEndDate);
    }

    [Fact]
    public void Constructor_StartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 12, 31, 0, 0, 0, Offset);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, Offset);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DateTimeOffsetRange(start, end));
    }

    [Fact]
    public void Constructor_StartAfterEnd_MessageIndicatesConstraint()
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() =>
            new DateTimeOffsetRange(new DateTimeOffset(2026, 12, 31, 0, 0, 0, Offset), new DateTimeOffset(2026, 1, 1, 0, 0, 0, Offset)));

        // Assert
        Assert.Contains("RangeStartDate", ex.Message);
    }
}