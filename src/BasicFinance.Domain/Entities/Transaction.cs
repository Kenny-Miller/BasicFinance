using BasicFinance.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicFinance.Domain.Entities
{
    public class Transaction : IEntity
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
        public Guid AccountId { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
        public DateTimeOffset SystemCreatedDate { get; set; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
