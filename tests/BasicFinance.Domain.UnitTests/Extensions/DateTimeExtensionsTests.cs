using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void StartOfWeek_Monday_ReturnsSameDate()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfWeek_Friday_ReturnsMonday()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfWeek_Sunday_ReturnsPreviousMonday()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 2, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(new DateTime(2026, 7, 27, 14, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfWeek_PreservesKind()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Local);

        // Act
        var result = dt.StartOfWeek;

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void StartOfMonth_MidMonth_ReturnsFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfMonth;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfMonth_AlreadyFirst_ReturnsSame()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfMonth;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfMonth_February_ReturnsFebruaryFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 2, 28, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfMonth;

        // Assert
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfMonth_PreservesKind()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Local);

        // Act
        var result = dt.StartOfMonth;

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void StartOfQuarter_January_ReturnsJanuaryFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfQuarter_April_ReturnsAprilFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfQuarter_July_ReturnsJulyFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfQuarter_October_ReturnsOctoberFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfQuarter_MidQuarter_SnapsBack()
    {
        // Arrange
        var dt = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfYear_AnyDate_ReturnsJanuaryFirst()
    {
        // Arrange
        var dt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.StartOfYear;

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void StartOfYear_PreservesKind()
    {
        // Arrange
        var dt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Local);

        // Act
        var result = dt.StartOfYear;

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void EndOfWeek_Monday_ReturnsSundayEndOfDay()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfWeek;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 9, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfWeek_Sunday_ReturnsSameDayEndOfDay()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 2, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfWeek;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 2, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfMonth_August_Returns31st()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfMonth;

        // Assert
        Assert.Equal(new DateTime(2026, 8, 31, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfMonth_February_NonLeapYear_Returns28th()
    {
        // Arrange
        var dt = new DateTime(2025, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfMonth;

        // Assert
        Assert.Equal(new DateTime(2025, 2, 28, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfMonth_February_LeapYear_Returns29th()
    {
        // Arrange
        var dt = new DateTime(2024, 2, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfMonth;

        // Assert
        Assert.Equal(new DateTime(2024, 2, 29, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfQuarter_Q1_ReturnsMarch31st()
    {
        // Arrange
        var dt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 3, 31, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfQuarter_Q2_ReturnsJune30th()
    {
        // Arrange
        var dt = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfQuarter;

        // Assert
        Assert.Equal(new DateTime(2026, 6, 30, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void EndOfYear_CommonYear_ReturnsDecember31st()
    {
        // Arrange
        var dt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.EndOfYear;

        // Assert
        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Theory]
    [InlineData(TimePeriod.Weekly, 2026, 8, 3)]
    [InlineData(TimePeriod.Monthly, 2026, 8, 1)]
    [InlineData(TimePeriod.Quarterly, 2026, 7, 1)]
    [InlineData(TimePeriod.Yearly, 2026, 1, 1)]
    public void ToStartOfPeriod_OffsetZero_ReturnsCorrectStart(TimePeriod period, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToStartOfPeriod(period, 0);

        // Assert
        Assert.Equal(expectedYear, result.Year);
        Assert.Equal(expectedMonth, result.Month);
        Assert.Equal(expectedDay, result.Day);
    }

    [Fact]
    public void ToStartOfPeriod_PositiveOffset_ReturnsFuturePeriod()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToStartOfPeriod(TimePeriod.Monthly, 1);

        // Assert
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToStartOfPeriod_NegativeOffset_ReturnsPastPeriod()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToStartOfPeriod(TimePeriod.Monthly, -1);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToStartOfPeriod_InvalidEnum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dt.ToStartOfPeriod((TimePeriod)999));
    }

    [Fact]
    public void ToEndOfPeriod_OffsetZero_ReturnsCorrectEnd()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToEndOfPeriod(TimePeriod.Monthly, 0);

        // Assert
        Assert.Equal(new DateTime(2026, 8, 31, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToEndOfPeriod_PositiveOffset_ReturnsFuturePeriodEnd()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToEndOfPeriod(TimePeriod.Monthly, 1);

        // Assert
        Assert.Equal(new DateTime(2026, 9, 30, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToEndOfPeriod_NegativeOffset_ReturnsPastPeriodEnd()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = dt.ToEndOfPeriod(TimePeriod.Monthly, -1);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 31, 23, 59, 59, 999, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToEndOfPeriod_InvalidEnum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dt.ToEndOfPeriod((TimePeriod)999));
    }

    [Theory]
    [InlineData(TimePeriod.Weekly)]
    [InlineData(TimePeriod.Monthly)]
    [InlineData(TimePeriod.Quarterly)]
    [InlineData(TimePeriod.Yearly)]
    public void ToPeriodRange_ReturnsValidRange(TimePeriod period)
    {
        // Arrange
        var dt = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var range = dt.ToPeriodRange(period);

        // Assert
        Assert.True(range.RangeStartDate <= range.RangeEndDate);
    }
}
