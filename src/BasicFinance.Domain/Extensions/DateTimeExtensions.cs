using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Internal;

namespace BasicFinance.Domain.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime source)
    {
        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the week it lies within.
        /// </summary>
        public DateTime StartOfWeek => source.AddDays(-(int)source.DayOfWeek);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the month it lies within.
        /// </summary>
        public DateTime StartOfMonth => new(source.Year, source.Month, 1, 0, 0, 0, source.Kind);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the quarter it lies within.
        /// </summary>
        public DateTime StartOfQuarter => new(source.Year, (source.Month + 3 - 1) / 3, 1, 0, 0, 0, source.Kind);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the start of the year it lies within.
        /// </summary>
        public DateTime StartOfYear => new(source.Year, 1, 1, 0, 0, 0, source.Kind);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the week it lies within.
        /// </summary>
        public DateTime EndOfWeek => source.StartOfWeek.AddDays(6).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the month it lies within.
        /// </summary>
        public DateTime EndOfMonth => source.StartOfMonth.AddMonths(1).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the quarter it lies within.
        /// </summary>
        public DateTime EndOfQuarter => source.StartOfQuarter.AddMonths(3).AddMilliseconds(-1);

        /// <summary>
        /// Gets a value containing <paramref name="source"/> adjusted to the end of the year it lies within.
        /// </summary>
        public DateTime EndOfYear => source.StartOfYear.AddYears(1).AddMilliseconds(-1);

        /// <summary>
        /// Returns the <see cref="DateTime"/> that represents the start of
        /// the specified period.
        /// </summary>
        /// <param name="timePeriod"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DateTime ToStartOfPeriod(TimePeriod timePeriod, int offset = 0)
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
        /// Returns the <see cref="DateTime"/> that represents the end of
        /// the specified period.
        /// </summary>
        /// <param name="timePeriod"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DateTime ToEndOfPeriod(TimePeriod timePeriod, int offset = 0)
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
        /// Returns a <see cref="DateTimeRange"/> that spans the specified period
        /// containing <paramref name="source"/>.
        /// </summary>
        /// <param name="timePeriod"></param>
        public DateTimeRange ToPeriodRange(TimePeriod timePeriod) => new(source.ToStartOfPeriod(timePeriod), source.ToEndOfPeriod(timePeriod));
    }
}