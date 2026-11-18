using BasicFinance.Domain.Internal;
using Xunit;

namespace BasicFinance.Domain.UnitTests.Internal;

public class LookbackWindowTests
{
    private static readonly DateTimeOffset EndExclusive = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_BoundedWindow_KeepsItsOwnStartAndIgnoresEarliestData()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var window = new LookbackWindow(start, EndExclusive);

        // Act
        var resolved = window.Resolve(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(start, resolved.Start);
        Assert.Equal(EndExclusive, resolved.EndExclusive);
        Assert.Equal(31, resolved.Days);
    }

    [Fact]
    public void Resolve_UnboundedWindowWithData_StartsAtEarliestData()
    {
        // Arrange
        var earliest = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var window = new LookbackWindow(null, EndExclusive);

        // Act
        var resolved = window.Resolve(earliest);

        // Assert
        Assert.Equal(earliest, resolved.Start);
        Assert.Equal(EndExclusive, resolved.EndExclusive);
    }

    [Fact]
    public void Resolve_UnboundedWindowWithNoData_CoversOnlyItsFinalDay()
    {
        // Arrange
        var window = new LookbackWindow(null, EndExclusive);

        // Act
        var resolved = window.Resolve(null);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero), resolved.Start);
        Assert.Equal(1, resolved.Days);
    }

    [Fact]
    public void Resolve_UnboundedWindowWhoseEarliestDataIsAfterTheWindow_CoversOnlyItsFinalDay()
    {
        // Arrange
        var window = new LookbackWindow(null, EndExclusive);

        // Act
        var resolved = window.Resolve(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(resolved.LastDay, resolved.Start);
        Assert.Equal(1, resolved.Days);
    }
}
