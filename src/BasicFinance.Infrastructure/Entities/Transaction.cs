using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Transaction : IEntity
    {
        [Key]
        public Guid Id { get; init; }

        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
        public Guid AccountId { get; set; }
        public DateTimeOffset Date { get; set; }

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Description { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Category { get; set; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
