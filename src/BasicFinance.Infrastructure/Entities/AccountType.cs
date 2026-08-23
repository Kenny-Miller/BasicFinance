using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BasicFinance.Infrastructure.Entities
{
    public class AccountType : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the id of the account type.
        /// </summary>
        public int AccountTypeId { get; set; }

        /// <summary>
        /// Gets a value indicating the account type code.
        /// </summary>
        [Required]
        [MaxLength(25)]
        public required string AccountTypeCode { get; set; }

        /// <summary>
        /// Gets a value indicating the account type name.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string AccountTypeName { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the balance of an account of this type
        /// is a liability (money owed) and therefore reduces net worth.
        /// </summary>
        public bool IsLiability { get; set; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation collection of <see cref="Account"/> entries associated with this account type.
        /// </summary>
        public ICollection<Account> Accounts { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountType"/> class.
        /// </summary>
        /// <param name="accountTypeCode">The account type code.</param>
        /// <param name="accountTypeName">The account type name.</param>
        /// <param name="isLiability">Whether balances of this type are liabilities that reduce net worth.</param>
        [SetsRequiredMembers]
        public AccountType(string accountTypeCode, string accountTypeName, bool isLiability = false)
        {
            AccountTypeCode = accountTypeCode;
            AccountTypeName = accountTypeName;
            IsLiability = isLiability;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountType"/> class for use by Entity Framework
        /// </summary>
        private AccountType()
        {
            // For Ef
        }
    }
}