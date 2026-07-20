namespace BasicFinance.Domain.Internal;

public class DateTimeOffsetRange
{
    public DateTimeOffset RangeStartDate { get; }
    public DateTimeOffset RangeEndDate { get; }

    public DateTimeOffsetRange(DateTimeOffset RangeStartDate, DateTimeOffset RangeEndDate)
    {
        if (RangeStartDate > RangeEndDate)
        {
            throw new ArgumentException("RangeStartDate must be less than or equal to RangeEndDate");
        }
    }
}