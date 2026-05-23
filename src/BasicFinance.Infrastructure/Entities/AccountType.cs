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
        /// <param name="accountTypeCode"></param>
        /// <param name="accountTypeName"></param>
        [SetsRequiredMembers]
        public AccountType(string accountTypeCode, string accountTypeName)
        {
            AccountTypeCode = accountTypeCode;
            AccountTypeName = accountTypeName;
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
