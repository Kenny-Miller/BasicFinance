namespace BasicFinance.Domain.Enums;

/// <summary>
/// A trailing window that ends at an anchor date and looks back a fixed distance from it.
/// Unlike <see cref="TimePeriod"/>, a lookback range is not aligned to the calendar.
/// </summary>
public enum LookbackRange
{
    /// <summary>
    /// The month ending at the anchor date.
    /// </summary>
    OneMonth = 1,

    /// <summary>
    /// The three months ending at the anchor date.
    /// </summary>
    ThreeMonths = 2,

    /// <summary>
    /// The six months ending at the anchor date.
    /// </summary>
    SixMonths = 3,

    /// <summary>
    /// The year ending at the anchor date.
    /// </summary>
    OneYear = 4,

    /// <summary>
    /// Everything up to the anchor date. Has no fixed start.
    /// </summary>
    All = 5,
}
