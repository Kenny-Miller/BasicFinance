using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Domain.UnitTests.Helpers;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Extensions;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void StartOfWeek_Monday_ReturnsSameDate()
    {
        // Arrange
        var dt = new DateTimeOffset(2026, 8, 3, 0, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void StartOfWeek_Friday_ReturnsMonday()
    {
        // Arrange
        var dt = new DateTimeOffset(2026, 8, 7, 0, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void StartOfWeek_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.StartOfWeek;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void StartOfMonth_MidMonth_ReturnsFirst()
    {
        // Arrange
        var dt = new DateTimeOffset(2026, 8, 15, 12, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.StartOfMonth;

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void StartOfMonth_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.StartOfMonth;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void StartOfQuarter_July_ReturnsJulyFirst()
    {
        // Arrange
        var dt = new DateTimeOffset(2026, 7, 15, 12, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void StartOfQuarter_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.StartOfQuarter;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void StartOfYear_ReturnsJanuaryFirst()
    {
        // Arrange
        var dt = new DateTimeOffset(2026, 6, 15, 12, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.StartOfYear;

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void StartOfYear_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.StartOfYear;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void EndOfWeek_Friday_ReturnsSundayEndOfDay()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfWeek;

        // Assert
        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
        Assert.Equal(999, result.Millisecond);
    }

    [Fact]
    public void EndOfWeek_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfWeek;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void EndOfMonth_August_Returns31st()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfMonth;

        // Assert
        Assert.Equal(31, result.Day);
        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
    }

    [Fact]
    public void EndOfMonth_February_LeapYear_Returns29th()
    {
        // Arrange
        var dt = new DateTimeOffset(2024, 2, 15, 12, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = dt.EndOfMonth;

        // Assert
        Assert.Equal(29, result.Day);
    }

    [Fact]
    public void EndOfMonth_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfMonth;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void EndOfQuarter_Q3_ReturnsSeptember30th()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfQuarter;

        // Assert
        Assert.Equal(9, result.Month);
        Assert.Equal(30, result.Day);
    }

    [Fact]
    public void EndOfQuarter_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfQuarter;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Fact]
    public void EndOfYear_ReturnsDecember31st()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfYear;

        // Assert
        Assert.Equal(12, result.Month);
        Assert.Equal(31, result.Day);
    }

    [Fact]
    public void EndOfYear_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.EndOfYear;

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, result.Offset);
    }

    [Theory]
    [InlineData(TimePeriod.Weekly, 2026, 8, 3)]
    [InlineData(TimePeriod.Monthly, 2026, 8, 1)]
    [InlineData(TimePeriod.Quarterly, 2026, 7, 1)]
    [InlineData(TimePeriod.Yearly, 2026, 1, 1)]
    public void ToStartOfPeriod_OffsetZero_ReturnsCorrectStart(TimePeriod period, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToStartOfPeriod(period, 0);

        // Assert
        Assert.Equal(expectedYear, result.Year);
        Assert.Equal(expectedMonth, result.Month);
        Assert.Equal(expectedDay, result.Day);
    }

    [Fact]
    public void ToStartOfPeriod_PositiveOffset_ReturnsFuturePeriod()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToStartOfPeriod(TimePeriod.Monthly, 1);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void ToStartOfPeriod_NegativeOffset_ReturnsPastPeriod()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToStartOfPeriod(TimePeriod.Monthly, -1);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void ToStartOfPeriod_InvalidEnum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => testDate.ToStartOfPeriod((TimePeriod)999));
    }

    [Fact]
    public void ToEndOfPeriod_OffsetZero_ReturnsCorrectEnd()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToEndOfPeriod(TimePeriod.Monthly, 0);

        // Assert
        Assert.Equal(31, result.Day);
        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
    }

    [Fact]
    public void ToEndOfPeriod_PositiveOffset_ReturnsFuturePeriodEnd()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToEndOfPeriod(TimePeriod.Monthly, 1);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void ToEndOfPeriod_NegativeOffset_ReturnsPastPeriodEnd()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = testDate.ToEndOfPeriod(TimePeriod.Monthly, -1);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 31, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result);
    }

    [Fact]
    public void ToEndOfPeriod_InvalidEnum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => testDate.ToEndOfPeriod((TimePeriod)999));
    }

    [Theory]
    [InlineData(TimePeriod.Weekly)]
    [InlineData(TimePeriod.Monthly)]
    [InlineData(TimePeriod.Quarterly)]
    [InlineData(TimePeriod.Yearly)]
    public void ToPeriodRange_ReturnsValidRange(TimePeriod period)
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var range = testDate.ToPeriodRange(period);

        // Assert
        Assert.True(range.RangeStartDate <= range.RangeEndDate);
    }

    [Fact]
    public void ToPeriodRange_PreservesOffset()
    {
        // Arrange
        var testDate = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var range = testDate.ToPeriodRange(TimePeriod.Monthly);

        // Assert
        Assert.Equal(DateTimeOffsetHelper.TestOffset, range.RangeStartDate.Offset);
        Assert.Equal(DateTimeOffsetHelper.TestOffset, range.RangeEndDate.Offset);
    }
}