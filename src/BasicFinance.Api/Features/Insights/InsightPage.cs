using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Domain.Internal;

namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Describes the window an insights response covers and how to reach the pages either side of it.
/// </summary>
/// <param name="WindowStart">The first UTC day the response covers.</param>
/// <param name="WindowEnd">The last UTC day the response covers, inclusive.</param>
/// <param name="PreviousAsOf">
/// The <c>asOf</c> value to request for the previous (older) page. <see langword="null"/> when no matching data
/// exists before <paramref name="WindowStart"/>.
/// </param>
/// <param name="NextAsOf">
/// The <c>asOf</c> value to request for the next (newer) page, never later than today. <see langword="null"/> when
/// this page already includes today.
/// </param>
public record InsightPage(DateTime WindowStart, DateTime WindowEnd, DateTime? PreviousAsOf, DateTime? NextAsOf)
{
    /// <summary>
    /// Builds the page for a calendar period. Neighbouring pages are the periods directly before and after it.
    /// </summary>
    /// <param name="window">The window of the period being reported.</param>
    /// <param name="timePeriod">The calendar period the window spans.</param>
    /// <param name="anchor">The resolved request day and the current day.</param>
    /// <param name="hasEarlierData">Whether any matching data exists before the window starts.</param>
    /// <returns>The page describing <paramref name="window"/>.</returns>
    internal static InsightPage ForPeriod(PeriodWindow window, TimePeriod timePeriod, InsightAnchor anchor, bool hasEarlierData)
    {
        DateTime? previousAsOf = hasEarlierData ? anchor.AsOf.ToStartOfPeriod(timePeriod, -1).UtcDateTime : null;
        DateTime? nextAsOf = window.EndExclusive <= anchor.Today ? window.EndExclusive.UtcDateTime : null;

        return new(window.Start.UtcDateTime, window.LastDay.UtcDateTime, previousAsOf, nextAsOf);
    }

    /// <summary>
    /// Builds the page for a trailing lookback window. The previous page ends the day before this one starts, and
    /// the next page ends one range length later, capped at today.
    /// </summary>
    /// <param name="window">The resolved window being reported.</param>
    /// <param name="range">The lookback range the window was built from.</param>
    /// <param name="anchor">The resolved request day and the current day.</param>
    /// <param name="hasEarlierData">Whether any matching data exists before the window starts.</param>
    /// <returns>The page describing <paramref name="window"/>.</returns>
    internal static InsightPage ForLookback(PeriodWindow window, LookbackRange range, InsightAnchor anchor, bool hasEarlierData)
    {
        DateTime? previousAsOf = hasEarlierData ? window.Start.AddDays(-1).UtcDateTime : null;
        DateTime? nextAsOf = null;
        if (window.EndExclusive <= anchor.Today)
        {
            var next = window.EndExclusive.AddLookbackRanges(range, 1)?.AddDays(-1) ?? anchor.Today;
            nextAsOf = (next > anchor.Today ? anchor.Today : next).UtcDateTime;
        }

        return new(window.Start.UtcDateTime, window.LastDay.UtcDateTime, previousAsOf, nextAsOf);
    }
}
