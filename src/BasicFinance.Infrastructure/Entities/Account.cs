using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Account : IEntity
    {
        [Key]
        public Guid AccountId { get; set; }

        [NotMapped]
        public Guid Id => AccountId;

        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        [Required]
        [MaxLength(255)]
        public required string AccountName { get; set; }

        [Precision(18, 2)]
        public decimal Balance { get; set; }

        [Required]
        [MaxLength(10)]
        public required string Currency { get; set; }

        [MaxLength(255)]
        public string? Notes { get; set; }
        public DateTimeOffset LastUpdatedDate { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Institution { get; set; }
        public Guid FinancialAccountId { get; set; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = [];
    }
}
