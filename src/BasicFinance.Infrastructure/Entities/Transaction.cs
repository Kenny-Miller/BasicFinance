using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using BasicFinance.Domain.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Transaction : IEntity
    {
        [Key]
        public Guid TransactionId { get; set; }

        [NotMapped]
        public Guid Id => TransactionId;

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
        public DateTimeOffset? SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Expression to map the <see cref="Transaction"/> into a <see cref="TransactionDto"/>.
        /// </summary>
        public static Expression<Func<Transaction, TransactionDto>> ToDtoExpression => x => ToDto(x);

        /// <summary>
        /// Maps to map the <see cref="Transaction"/> into a <see cref="TransactionDto"/>.
        /// </summary>
        public static Func<Transaction, TransactionDto> ToDto =
            (x) => new(x.TransactionId, x.Date, x.Amount, x.Description, x.Category, x.SystemCreatedDate);
    }
}
