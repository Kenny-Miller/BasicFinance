using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Internal;

namespace BasicFinance.Domain.Extensions;

public static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset source)
    {
        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the week it lies within.
        /// </summary>
        public DateTimeOffset StartOfWeek => source.AddDays(-(int)source.DayOfWeek);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the month it lies within.
        /// </summary>
        public DateTimeOffset StartOfMonth => new(source.Year, source.Month, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the quarter it lies within.
        /// </summary>
        public DateTimeOffset StartOfQuarter => new(source.Year, (source.Month + 3 - 1) / 3, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the year it lies within.
        /// </summary>
        public DateTimeOffset StartOfYear => new(source.Year, 1, 1, 0, 0, 0, source.Offset);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the week it lies within.
        /// </summary>
        public DateTimeOffset EndOfWeek => source.StartOfWeek.AddDays(6).AddMilliseconds(-1);

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
        /// the specified period.
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
                TimePeriod.Weekly => source.StartOfWeek.AddDays(offset * 7).AddDays(6).AddMilliseconds(-1),
                TimePeriod.Monthly => source.StartOfMonth.AddMonths(offset).AddMonths(1).AddMilliseconds(-1),
                TimePeriod.Quarterly => source.StartOfQuarter.AddMonths(offset * 3).AddMonths(3).AddMilliseconds(-1),
                TimePeriod.Yearly => source.StartOfYear.AddYears(offset).AddYears(1).AddMilliseconds(-1),
                _ => throw new ArgumentOutOfRangeException(nameof(timePeriod), timePeriod, null),
            };
        }

        /// <summary>
        /// Returns a <see cref="DateTimeOffsetRange"/> that spans the specified period
        /// containing <paramref name="source"/>.
        /// </summary>
        /// <param name="timePeriod"></param>
        public DateTimeOffsetRange ToPeriodRange(TimePeriod timePeriod) => new(source.ToStartOfPeriod(timePeriod), source.ToEndOfPeriod(timePeriod));
    }
}