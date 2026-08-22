using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Domain.UnitTests.Helpers;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Extensions;

public class TimePeriodExtensionsTests
{
    [Theory]
    [InlineData(TimePeriod.Weekly, 2026, 8, 3, 2026, 8, 10, 2026, 7, 27, 2026, 8, 3)]
    [InlineData(TimePeriod.Monthly, 2026, 8, 1, 2026, 9, 1, 2026, 7, 1, 2026, 8, 1)]
    [InlineData(TimePeriod.Quarterly, 2026, 7, 1, 2026, 10, 1, 2026, 4, 1, 2026, 7, 1)]
    [InlineData(TimePeriod.Yearly, 2026, 1, 1, 2027, 1, 1, 2025, 1, 1, 2026, 1, 1)]
    public void ToPeriodResolution_AnchorDate_ReturnsCurrentAndPreviousBoundaries(
        TimePeriod period,
        int currentStartYear,
        int currentStartMonth,
        int currentStartDay,
        int currentEndYear,
        int currentEndMonth,
        int currentEndDay,
        int previousStartYear,
        int previousStartMonth,
        int previousStartDay,
        int previousEndYear,
        int previousEndMonth,
        int previousEndDay)
    {
        // Arrange
        var anchor = new DateTimeOffset(2026, 8, 7, 12, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = period.ToPeriodResolution(anchor, DateTimeOffset.MinValue);

        // Assert
        Assert.Equal(new DateOnly(currentStartYear, currentStartMonth, currentStartDay), result.CurrentStart);
        Assert.Equal(new DateOnly(currentEndYear, currentEndMonth, currentEndDay), result.CurrentEnd);
        Assert.Equal(new DateOnly(previousStartYear, previousStartMonth, previousStartDay), result.PreviousStart);
        Assert.Equal(new DateOnly(previousEndYear, previousEndMonth, previousEndDay), result.PreviousEnd);
        Assert.True(result.CurrentRange.RangeStartDate < result.CurrentRange.RangeEndDate);
        Assert.True(result.PreviousRange.RangeStartDate < result.PreviousRange.RangeEndDate);
        Assert.Equal(result.PreviousRange.RangeEndDate, result.CurrentRange.RangeStartDate.AddMilliseconds(-1));
    }

    [Fact]
    public void ToPeriodResolution_Monthly_ReturnsMonthlyRanges()
    {
        // Arrange
        var anchor = DateTimeOffsetHelper.CreateTestDate();

        // Act
        var result = TimePeriod.Monthly.ToPeriodResolution(anchor, anchor.AddYears(1));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result.CurrentRange.RangeStartDate);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result.CurrentRange.RangeEndDate);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result.PreviousRange.RangeStartDate);
        Assert.Equal(new DateTimeOffset(2026, 7, 31, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result.PreviousRange.RangeEndDate);
    }

    [Fact]
    public void ToPeriodResolution_Weekly_ReturnsMondayBasedWeeks()
    {
        // Arrange
        var anchor = new DateTimeOffset(2026, 8, 3, 6, 0, 0, DateTimeOffsetHelper.TestOffset);

        // Act
        var result = TimePeriod.Weekly.ToPeriodResolution(anchor, anchor.AddYears(1));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result.CurrentRange.RangeStartDate);
        Assert.Equal(new DateTimeOffset(2026, 8, 9, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result.CurrentRange.RangeEndDate);
        Assert.Equal(new DateTimeOffset(2026, 7, 27, 0, 0, 0, DateTimeOffsetHelper.TestOffset), result.PreviousRange.RangeStartDate);
        Assert.Equal(new DateTimeOffset(2026, 8, 2, 23, 59, 59, 999, DateTimeOffsetHelper.TestOffset), result.PreviousRange.RangeEndDate);
    }

    [Fact]
    public void ToPeriodResolution_NullAnchor_UsesFallbackNow()
    {
        // Arrange
        var fallbackNow = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var result = TimePeriod.Monthly.ToPeriodResolution(null, fallbackNow);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 2, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2025, 12, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 1, 1), result.PreviousEnd);
    }

    [Fact]
    public void ToPeriodResolution_JanuaryAnchor_ReturnsPreviousQuarterInPriorYear()
    {
        // Arrange
        var anchor = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

        // Act
        var result = TimePeriod.Quarterly.ToPeriodResolution(anchor, anchor.AddYears(1));

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 4, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2025, 10, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 1, 1), result.PreviousEnd);
    }

    [Fact]
    public void ToPeriodResolution_YearlyAcrossYearEnd_ReturnsPriorYearAsPrevious()
    {
        // Arrange
        var anchor = new DateTimeOffset(2026, 12, 25, 18, 0, 0, TimeSpan.Zero);

        // Act
        var result = TimePeriod.Yearly.ToPeriodResolution(anchor, anchor.AddYears(1));

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2027, 1, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2025, 1, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 1, 1), result.PreviousEnd);
    }

    [Fact]
    public void ToPeriodResolution_InvalidPeriod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var anchor = DateTimeOffsetHelper.CreateTestDate();
        var invalidPeriod = (TimePeriod)0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidPeriod.ToPeriodResolution(anchor, anchor));
    }
}
