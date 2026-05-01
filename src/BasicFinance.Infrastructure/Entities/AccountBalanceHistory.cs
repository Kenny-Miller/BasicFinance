using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class AccountBalanceHistory : IEntity
    {
        [Key]
        public Guid AccountBalanceHistoryId { get; set; }

        [NotMapped]
        public Guid Id => AccountBalanceHistoryId;

        public Guid AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        [Precision(18, 2)]
        public decimal Balance { get; set; }
        public DateTimeOffset BalanceRecordedDate { get; set; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset? SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }

        public AccountBalanceHistory()
        {
        }

        public AccountBalanceHistory(Account account)
        {
            AccountId = account.AccountId;
            Account = account;
            Balance = account.Balance;
            BalanceRecordedDate = account.BalanceRecordedDate;
            SystemCreatedDate = DateTimeOffset.UtcNow;
            IsActive = true;
        }
    }
}
