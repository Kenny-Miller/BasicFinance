namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Shared date handling for the insights endpoints.
/// </summary>
internal static class InsightDates
{
    /// <summary>
    /// Resolves the optional <c>AsOf</c> request value to a whole UTC day, defaulting to and never exceeding today.
    /// </summary>
    /// <remarks>
    /// The time of day is always discarded. A value bound with an offset (for example
    /// <c>2026-10-01T00:00:00Z</c>) arrives as local time, so it is converted back to UTC before the day is taken.
    /// Values without an offset are treated as UTC.
    /// </remarks>
    /// <param name="asOf">Any moment within the requested day, or <see langword="null"/> for today.</param>
    /// <param name="timeProvider">Time provider supplying the current time.</param>
    /// <returns>The requested day clamped to today, together with today.</returns>
    public static InsightAnchor Resolve(DateTime? asOf, TimeProvider timeProvider)
    {
        var today = new DateTimeOffset(timeProvider.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
        if (!asOf.HasValue)
        {
            return new(today, today);
        }

        var requested = asOf.Value.Kind == DateTimeKind.Local
            ? asOf.Value.ToUniversalTime()
            : asOf.Value;
        var day = new DateTimeOffset(DateTime.SpecifyKind(requested.Date, DateTimeKind.Utc), TimeSpan.Zero);
        return new(day > today ? today : day, today);
    }
}
