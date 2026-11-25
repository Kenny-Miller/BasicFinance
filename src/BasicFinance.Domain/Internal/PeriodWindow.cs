namespace BasicFinance.Domain.Internal;

/// <summary>
/// A half-open span of time: includes <paramref name="Start"/> and everything up to, but not including,
/// <paramref name="EndExclusive"/>.
/// </summary>
/// <param name="Start">The first instant inside the window.</param>
/// <param name="EndExclusive">The first instant after the window.</param>
public readonly record struct PeriodWindow(DateTimeOffset Start, DateTimeOffset EndExclusive)
{
    /// <summary>
    /// Gets the number of whole days the window spans.
    /// </summary>
    public int Days => (EndExclusive - Start).Days;

    /// <summary>
    /// Gets the last whole day inside the window.
    /// </summary>
    public DateTimeOffset LastDay => EndExclusive.AddDays(-1);
}
