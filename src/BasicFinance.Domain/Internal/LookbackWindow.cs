namespace BasicFinance.Domain.Internal;

/// <summary>
/// A trailing window that may have no fixed start. A <see langword="null"/> <paramref name="Start"/> means the
/// window reaches back to the earliest available data.
/// </summary>
/// <param name="Start">The first instant inside the window, or <see langword="null"/> when unbounded.</param>
/// <param name="EndExclusive">The first instant after the window.</param>
public readonly record struct LookbackWindow(DateTimeOffset? Start, DateTimeOffset EndExclusive)
{
    /// <summary>
    /// Gets a value indicating whether the window has no fixed start.
    /// </summary>
    public bool IsUnbounded => Start is null;

    /// <summary>
    /// Turns the window into a concrete <see cref="PeriodWindow"/>. An unbounded window starts at
    /// <paramref name="earliestData"/>, or covers just its final day when there is no data at all.
    /// </summary>
    /// <param name="earliestData">The start of the day holding the earliest data, if any exists.</param>
    /// <returns>A window with a definite start.</returns>
    public PeriodWindow Resolve(DateTimeOffset? earliestData)
    {
        var lastDay = EndExclusive.AddDays(-1);
        var start = Start ?? earliestData ?? lastDay;
        return new(start > lastDay ? lastDay : start, EndExclusive);
    }
}
