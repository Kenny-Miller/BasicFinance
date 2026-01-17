namespace BasicFinance.Domain.Interfaces
{
    public interface IEntity
    {
        public Guid Id { get; init; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
