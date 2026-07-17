using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BasicFinance.Infrastructure.Entities
{
    public class TransactionCategory : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the id of the transaction type.
        /// </summary>
        [Key]
        public int TransactionCategoryId { get; set; }

        /// <summary>
        /// Gets a value indicating the transaction category code.
        /// </summary>
        [Required]
        [MaxLength(25)]
        public required string TransactionCategoryCode { get; set; }

        /// <summary>
        /// Gets a value indicating the transaction category name.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string TransactionCategoryName { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation collection of <see cref="Transaction"/> entries associated with this transaction category.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionCategory"/> class.
        /// </summary>
        /// <param name="accountTypeCode"></param>
        /// <param name="accountTypeName"></param>
        [SetsRequiredMembers]
        public TransactionCategory(string transactionCategoryCode, string transactionCategoryName)
        {
            TransactionCategoryCode = transactionCategoryCode;
            TransactionCategoryName = transactionCategoryName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionCategory"/> class for use by Entity Framework
        /// </summary>
        private TransactionCategory()
        {
            // For Ef
        }
    }
}