using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Account : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the unique identifier of the account.
        /// </summary>
        [Key]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public Guid Id => AccountId;

        /// <summary>
        /// Gets a value indicating the unique identifier of the associated <see cref="UserGoogleSpreadsheet"/>.
        /// </summary>
        public Guid UserGoogleSpreadsheetId { get; init; }

        /// <summary>
        /// Gets a value indicating the associated <see cref="UserGoogleSpreadsheet"/> for the account.
        /// </summary>
        [ForeignKey(nameof(UserGoogleSpreadsheetId))]
        public UserGoogleSpreadsheet UserGoogleSpreadsheet { get; init; } = null!;

        /// <summary>
        /// Gets a value indicating the unique identifier of the user who owns the account.
        /// </summary>
        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        /// <summary>
        /// Gets a value indicating the name of the account (e.g., "Checking Account", "Savings Account").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public required string AccountName { get; init; }

        /// <summary>
        /// Gets a value indicating the current balance of the account.
        /// </summary>
        [Precision(18, 2)]
        public decimal Balance { get; private set; }

        /// <summary>
        /// Gets a value indicating the currency of the account balance (e.g., "USD", "EUR").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public required string Currency { get; init; }

        /// <summary>
        /// Gets a value indicating any additional notes or information about the account.
        /// </summary>
        [MaxLength(255)]
        public string? Notes { get; init; }

        /// <summary>
        /// Gets a value indicating the date and time when the account balance was recorded.
        /// </summary>
        public DateTimeOffset BalanceRecordedDate { get; private set; }

        /// <summary>
        /// Gets a value indicating the financial institution associated with the account (e.g., "Bank of America", "Chase").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public required string Institution { get; init; }

        /// <summary>
        /// Gets a value indicating the unique identifier of the financial account associated with the account.
        /// </summary>
        public Guid FinancialAccountId { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation collection of <see cref="AccountBalanceHistory"/> entries associated with this account.
        /// </summary>
        public ICollection<AccountBalanceHistory> AccountBalanceHistory { get; set; } = [];

        /// <summary>
        /// Navigation collection of <see cref="Transaction"/> entries associated with this account.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; } = [];

        /// <summary>
        ///  Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="userGoogleSpreadsheetId"></param>
        /// <param name="userId"></param>
        /// <param name="accountName></param>
        /// <param name="balance"></param>
        /// <param name="currency"></param>
        /// <param name="notes"></param>
        /// <param name="institution"></param>
        /// <param name="financialAccountId"></param>
        /// <param name="balanceRecordedDate"></param>
        [SetsRequiredMembers]
        public Account(
            Guid userGoogleSpreadsheetId,
            string userId,
            string accountName,
            decimal balance,
            string currency,
            string notes,
            string institution,
            Guid financialAccountId,
            DateTimeOffset balanceRecordedDate)
        {
            UserGoogleSpreadsheetId = userGoogleSpreadsheetId;
            UserId = userId;
            AccountName = accountName;
            Balance = balance;
            Currency = currency;
            Notes = notes;
            BalanceRecordedDate = balanceRecordedDate;
            Institution = institution;
            FinancialAccountId = financialAccountId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class for use by Entity Framework.
        /// </summary>
        private Account()
        {
            // For EF
        }

        /// <summary>
        /// Updates the balance and the date the balance was recorded for the account.
        /// </summary>
        /// <param name="newBalance"></param>
        /// <param name="balanceRecordedDate"></param>
        public void UpdateBalance(decimal newBalance, DateTimeOffset balanceRecordedDate)
        {
            Balance = newBalance;
            BalanceRecordedDate = balanceRecordedDate;
            SystemModifiedDate = DateTimeOffset.UtcNow;
        }
    }
}
