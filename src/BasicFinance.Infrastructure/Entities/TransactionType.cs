using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BasicFinance.Infrastructure.Entities
{
    public class TransactionType : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the id of the transaction type.
        /// </summary>
        [Key]
        public int TransactionTypeId { get; set; }

        /// <summary>
        /// Gets a value indicating the transaction type code.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public required string TransactionTypeCode { get; set; }

        /// <summary>
        /// Gets a value indicating the transaction type name.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public required string TransactionTypeName { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation collection of <see cref="Transaction"/> entries associated with this transaction type.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionType"/> class.
        /// </summary>
        /// <param name="accountTypeCode"></param>
        /// <param name="accountTypeName"></param>
        [SetsRequiredMembers]
        public TransactionType(string transactionTypeCode, string transactionTypeName)
        {
            TransactionTypeCode = transactionTypeCode;
            TransactionTypeName = transactionTypeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionType"/> class for use by Entity Framework
        /// </summary>
        private TransactionType()
        {
            // For Ef
        }
    }
}
