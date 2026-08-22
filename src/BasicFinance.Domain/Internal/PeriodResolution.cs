namespace BasicFinance.Domain.Internal;

/// <summary>
/// Represents the resolved current and previous period ranges for a period-mode
/// query, together with their date-only boundaries for display.
/// </summary>
/// <param name="CurrentRange">The current period range.</param>
/// <param name="PreviousRange">The period immediately preceding <paramref name="CurrentRange"/>.</param>
/// <param name="CurrentStart">The first day of the current period (inclusive).</param>
/// <param name="CurrentEnd">The first day excluded from the current period (exclusive).</param>
/// <param name="PreviousStart">The first day of the previous period (inclusive).</param>
/// <param name="PreviousEnd">The first day excluded from the previous period (exclusive).</param>
public record PeriodResolution(
    DateTimeOffsetRange CurrentRange,
    DateTimeOffsetRange PreviousRange,
    DateOnly CurrentStart,
    DateOnly CurrentEnd,
    DateOnly PreviousStart,
    DateOnly PreviousEnd);
