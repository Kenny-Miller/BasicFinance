using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Internal;

namespace BasicFinance.Domain.Extensions;

public static class TimePeriodExtensions
{
    /// <summary>
    /// Resolves the current and previous period ranges around an anchor date,
    /// together with their date-only boundaries.
    /// </summary>
    /// <param name="timePeriod">The period used to compute the ranges.</param>
    /// <param name="anchor">The anchor date the periods are computed around. Falls back to <paramref name="fallbackNow"/> when <see langword="null"/>.</param>
    /// <param name="fallbackNow">The date used when <paramref name="anchor"/> is <see langword="null"/>.</param>
    /// <returns>
    /// A <see cref="PeriodResolution"/> containing the current and previous period ranges
    /// and their inclusive start / exclusive end date boundaries.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timePeriod"/> is not a valid <see cref="TimePeriod"/> value.
    /// </exception>
    public static PeriodResolution ToPeriodResolution(this TimePeriod timePeriod, DateTimeOffset? anchor, DateTimeOffset fallbackNow)
    {
        var date = anchor ?? fallbackNow;
        var currentRange = date.ToPeriodRange(timePeriod);
        var previousRange = date.ToPeriodRange(timePeriod, -1);

        return new PeriodResolution(
            currentRange,
            previousRange,
            ToDay(currentRange.RangeStartDate),
            ToDay(currentRange.RangeEndDate).AddDays(1),
            ToDay(previousRange.RangeStartDate),
            ToDay(previousRange.RangeEndDate).AddDays(1));
    }

    private static DateOnly ToDay(DateTimeOffset value) => new(value.Year, value.Month, value.Day);
}
