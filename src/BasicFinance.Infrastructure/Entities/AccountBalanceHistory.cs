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

        [ForeignKey(nameof(AccountId))]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public Account Account { get; set; } = null!;
        public Guid AccountId { get; set; }

        [Precision(18, 2)]
        public decimal Balance { get; set; }
        public DateTimeOffset BalanceRecordedDate { get; set; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset? SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }

        public AccountBalanceHistory(Account account)
        {
            AccountId = account.AccountId;
            Account = account;
            Balance = account.Balance;
            BalanceRecordedDate = account.BalanceRecordedDate;
            SystemCreatedDate = DateTimeOffset.UtcNow;
            IsActive = true;
        }

        private AccountBalanceHistory()
        {
            // For EF
        }
    }
}
