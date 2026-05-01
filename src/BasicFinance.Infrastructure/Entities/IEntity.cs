namespace BasicFinance.Infrastructure.Entities
{
    public interface IEntity
    {
        public Guid Id { get; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset? SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
