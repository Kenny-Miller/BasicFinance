using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Internal;

namespace BasicFinance.Domain.Extensions;

public static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset source)
    {
        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to midnight at the start of its day.
        /// </summary>
        public DateTimeOffset StartOfDay => new(source.Year, source.Month, source.Day, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the week it lies within, at midnight (00:00:00).
        /// </summary>
        public DateTimeOffset StartOfWeek
        {
            get
            {
                var startOfWeek = source.AddDays(-(source.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)source.DayOfWeek - 1));
                return new DateTimeOffset(startOfWeek.Year, startOfWeek.Month, startOfWeek.Day, 0, 0, 0, startOfWeek.Offset);
            }
        }

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the month it lies within.
        /// </summary>
        public DateTimeOffset StartOfMonth => new(source.Year, source.Month, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the quarter it lies within.
        /// </summary>
        public DateTimeOffset StartOfQuarter => new(source.Year, (source.Month - 1) / 3 * 3 + 1, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the year it lies within.
        /// </summary>
        public DateTimeOffset StartOfYear => new(source.Year, 1, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the week it lies within.
        /// </summary>
        public DateTimeOffset EndOfWeek => source.StartOfWeek.AddDays(7).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the month it lies within.
        /// </summary>
        public DateTimeOffset EndOfMonth => source.StartOfMonth.AddMonths(1).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the quarter it lies within.
        /// </summary>
        public DateTimeOffset EndOfQuarter => source.StartOfQuarter.AddMonths(3).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the year it lies within.
        /// </summary>
        public DateTimeOffset EndOfYear => source.StartOfYear.AddYears(1).AddMilliseconds(-1);

        /// <summary>
        /// Returns the <see cref="DateTimeOffset"/> that represents the start of
        /// the specified period, at the start of the day.
        /// </summary>
        /// <param name="timePeriod"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DateTimeOffset ToStartOfPeriod(TimePeriod timePeriod, int offset = 0)
        {
            return timePeriod switch
            {
                TimePeriod.Weekly => source.StartOfWeek.AddDays(offset * 7),
                TimePeriod.Monthly => source.StartOfMonth.AddMonths(offset),
                TimePeriod.Quarterly => source.StartOfQuarter.AddMonths(offset * 3),
                TimePeriod.Yearly => source.StartOfYear.AddYears(offset),
                _ => throw new ArgumentOutOfRangeException(nameof(timePeriod), timePeriod, null),
            };
        }

        /// <summary>
        /// Returns the <see cref="DateTimeOffset"/> that represents the end of
        /// the specified period.
        /// </summary>
        /// <param name="timePeriod"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DateTimeOffset ToEndOfPeriod(TimePeriod timePeriod, int offset = 0)
        {
            return timePeriod switch
            {
                TimePeriod.Weekly => source.StartOfWeek.AddDays(offset * 7).EndOfWeek,
                TimePeriod.Monthly => source.StartOfMonth.AddMonths(offset).EndOfMonth,
                TimePeriod.Quarterly => source.StartOfQuarter.AddMonths(offset * 3).EndOfQuarter,
                TimePeriod.Yearly => source.StartOfYear.AddYears(offset).EndOfYear,
                _ => throw new ArgumentOutOfRangeException(nameof(timePeriod), timePeriod, null),
            };
        }

        /// <summary>
        /// Returns a <see cref="DateTimeOffsetRange"/> that spans the specified period
        /// containing <paramref name="source"/>.
        /// </summary>
        /// <param name="timePeriod"></param>
        /// <param name="offset"></param>
        public DateTimeOffsetRange ToPeriodRange(TimePeriod timePeriod, int offset = 0) =>
            new(source.ToStartOfPeriod(timePeriod, offset), source.ToEndOfPeriod(timePeriod, offset));

        /// <summary>
        /// Returns the half-open <see cref="PeriodWindow"/> of the calendar period containing
        /// <paramref name="source"/>, shifted by <paramref name="offset"/> whole periods.
        /// </summary>
        /// <param name="timePeriod">The calendar period to span.</param>
        /// <param name="offset">How many periods to move forward (positive) or back (negative).</param>
        /// <returns>A window from the start of the period up to the start of the next one.</returns>
        public PeriodWindow ToPeriodWindow(TimePeriod timePeriod, int offset = 0) =>
            new(source.ToStartOfPeriod(timePeriod, offset), source.ToStartOfPeriod(timePeriod, offset + 1));

        /// <summary>
        /// Returns the trailing <see cref="LookbackWindow"/> whose final day is the day containing
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="range">How far the window looks back.</param>
        /// <returns>
        /// A window ending after the day of <paramref name="source"/>. Its start is <see langword="null"/> for
        /// <see cref="LookbackRange.All"/>.
        /// </returns>
        public LookbackWindow ToLookbackWindow(LookbackRange range)
        {
            var endExclusive = source.StartOfDay.AddDays(1);
            return new(endExclusive.AddLookbackRanges(range, -1), endExclusive);
        }

        /// <summary>
        /// Moves <paramref name="source"/> by <paramref name="count"/> lengths of <paramref name="range"/>.
        /// </summary>
        /// <param name="range">The range whose length is the unit of movement.</param>
        /// <param name="count">How many range lengths to move forward (positive) or back (negative).</param>
        /// <returns>
        /// The moved value, or <see langword="null"/> for <see cref="LookbackRange.All"/>, which has no length.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="range"/> is not defined.</exception>
        public DateTimeOffset? AddLookbackRanges(LookbackRange range, int count)
        {
            return range switch
            {
                LookbackRange.OneMonth => source.AddMonths(count),
                LookbackRange.ThreeMonths => source.AddMonths(count * 3),
                LookbackRange.SixMonths => source.AddMonths(count * 6),
                LookbackRange.OneYear => source.AddYears(count),
                LookbackRange.All => null,
                _ => throw new ArgumentOutOfRangeException(nameof(range), range, null),
            };
        }
    }
}