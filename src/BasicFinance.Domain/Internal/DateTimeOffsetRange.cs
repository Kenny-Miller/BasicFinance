namespace BasicFinance.Domain.Internal;

public class DateTimeOffsetRange
{
    public DateTimeOffset RangeStartDate { get; }
    public DateTimeOffset RangeEndDate { get; }

    public DateTimeOffsetRange(DateTimeOffset rangeStartDate, DateTimeOffset rangeEndDate)
    {
        if (rangeStartDate > rangeEndDate)
        {
            throw new ArgumentException("RangeStartDate must be less than or equal to RangeEndDate");
        }

        RangeStartDate = rangeStartDate;
        RangeEndDate = rangeEndDate;
    }
}