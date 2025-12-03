namespace BasicFinance.Domain.Interfaces
{
    public interface IEntity
    {
        public Guid Id { get; set; }
        public DateTimeOffset SystemCreatedDate { get; set; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
