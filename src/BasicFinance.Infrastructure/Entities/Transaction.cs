using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Transaction : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the unique identifier of the transaction.
        /// </summary>
        [Key]
        public Guid TransactionId { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public Guid Id => TransactionId;

        /// <summary>
        /// Gets a value indicating the unique identifier of the user who owns the transaction.
        /// </summary>
        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        /// <summary>
        /// Gets a value indicating the associated <see cref="Account"/> for the transaction.
        /// </summary>
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; init; } = null!;

        /// <summary>
        /// Gets a value indicating the unique identifier of the associated <see cref="Account"/> for the transaction.
        /// </summary>
        public Guid AccountId { get; init; }

        /// <summary>
        /// Gets a value indicating the date and time when the transaction occurred.
        /// </summary>
        public DateTimeOffset Date { get; init; }

        /// <summary>
        /// Gets a value indicating the amount of the transaction.
        /// </summary>
        [Precision(18, 2)]
        public decimal Amount { get; init; }

        [Required]
        [MaxLength(255)]
        public required string Description { get; init; }

        /// <summary>
        /// Gets a value indicating the category of the transaction (e.g., "Groceries", "Utilities", "Entertainment").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public required string Category { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        ///  Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <param name="date"></param>
        /// <param name="amount"></param>
        /// <param name="description"></param>
        /// <param name="category"></param>
        [SetsRequiredMembers]
        public Transaction(string userId, Guid accountId, DateTimeOffset date, decimal amount, string description, string category)
        {
            TransactionId = Guid.NewGuid();
            UserId = userId;
            AccountId = accountId;
            Date = date;
            Amount = amount;
            Description = description;
            Category = category;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class for use by Entity Framework.
        /// </summary>
        private Transaction()
        {
            // For EF
        }
    }
}
