namespace BasicFinance.Domain.UnitTests.Helpers;

internal static class DateTimeOffsetHelper
{
    internal static readonly TimeSpan TestOffset = TimeSpan.FromHours(-5);

    internal static DateTimeOffset CreateTestDate() =>
        new(2026, 8, 7, 12, 0, 0, TestOffset);
}