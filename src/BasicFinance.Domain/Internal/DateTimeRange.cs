namespace BasicFinance.Domain.Internal;

public record DateTimeRange
{
    public DateTime RangeStartDate { get; }
    public DateTime RangeEndDate { get; }

    public DateTimeRange(DateTime rangeStartDate, DateTime rangeEndDate)
    {
        if (rangeStartDate > rangeEndDate)
        {
            throw new ArgumentException("RangeStartDate must be less than or equal to RangeEndDate");
        }

        RangeStartDate = rangeStartDate;
        RangeEndDate = rangeEndDate;
    }
}