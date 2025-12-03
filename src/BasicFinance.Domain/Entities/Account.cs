using BasicFinance.Domain.Interfaces;

namespace BasicFinance.Domain.Entities
{
    public class Account : IEntity
    {
        public Guid Id { get; set; }
        public required string AccountName { get; set; }
        public decimal Balance { get; set; }
        public required string Currency { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset LastUpdatedDate { get; set; }
        public required string Institution { get; set; }
        public Guid AccountId { get; set; }
        public decimal AvailableBalance { get; set; }
        public DateTimeOffset SystemCreatedDate { get; set; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = [];
    }
}
