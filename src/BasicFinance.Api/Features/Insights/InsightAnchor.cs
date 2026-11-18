namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// The day an insights request is evaluated for, alongside the current day.
/// </summary>
/// <param name="AsOf">Midnight UTC on the requested day. Never later than <paramref name="Today"/>.</param>
/// <param name="Today">Midnight UTC on the current day.</param>
internal readonly record struct InsightAnchor(DateTimeOffset AsOf, DateTimeOffset Today);
