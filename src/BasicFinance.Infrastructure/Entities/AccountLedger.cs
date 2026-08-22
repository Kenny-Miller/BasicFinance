using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    /// <summary>
    /// Represents an append-only balance entry for an <see cref="Account"/>.
    /// Each entry records the balance as of its <see cref="BalanceRecordedDate"/>;
    /// the current balance is the entry with the greatest recorded date. Entries are immutable.
    /// </summary>
    public class AccountLedger
    {
        /// <summary>
        /// Gets or sets the unique identifier of the ledger entry.
        /// </summary>
        [Key]
        public Guid AccountLedgerId { get; set; }

        /// <summary>
        /// Gets a value indicating the unique identifier of the associated <see cref="Account"/>.
        /// </summary>
        public Guid AccountId { get; init; }

        /// <summary>
        /// Gets a value indicating the associated <see cref="Account"/> for the ledger entry.
        /// </summary>
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; init; } = null!;

        /// <summary>
        /// Gets a value indicating the balance of the account.
        /// </summary>
        [Precision(18, 2)]
        public decimal Balance { get; init; }

        /// <summary>
        /// Gets a value indicating the date and time the balance reflects account state as of.
        /// </summary>
        public DateTimeOffset BalanceRecordedDate { get; init; }

        /// <summary>
        /// Gets a value indicating the date and time the ledger entry was created.
        /// </summary>
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountLedger"/> class for the given <see cref="Account"/>.
        /// </summary>
        /// <param name="account">The account the balance entry belongs to.</param>
        /// <param name="balance">The balance of the account.</param>
        /// <param name="balanceRecordedDate">The date and time the balance reflects account state as of.</param>
        public AccountLedger(Account account, decimal balance, DateTimeOffset balanceRecordedDate)
        {
            Account = account;
            AccountId = account.AccountId;
            Balance = balance;
            BalanceRecordedDate = balanceRecordedDate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountLedger"/> class for use by Entity Framework.
        /// </summary>
        private AccountLedger()
        {
            // For EF
        }
    }
}
