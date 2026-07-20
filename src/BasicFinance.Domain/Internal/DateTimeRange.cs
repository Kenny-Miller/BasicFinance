namespace BasicFinance.Domain.Internal;

public record DateTimeRange
{
    public DateTime RangeStartDate { get; }
    public DateTime RangeEndDate { get; }

    public DateTimeRange(DateTime RangeStartDate, DateTime RangeEndDate)
    {
        if (RangeStartDate > RangeEndDate)
        {
            throw new ArgumentException("RangeStartDate must be less than or equal to RangeEndDate");
        }
    }
}