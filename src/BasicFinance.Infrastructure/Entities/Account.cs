using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure.Entities
{
    public class Account : IEntity
    {
        [Key]
        public Guid AccountId { get; set; }

        [NotMapped]
        public Guid Id => AccountId;

        public Guid UserGoogleSpreadsheetId { get; set; }

        [ForeignKey(nameof(UserGoogleSpreadsheetId))]
        public UserGoogleSpreadsheet UserGoogleSpreadsheet { get; set; } = null!;

        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        [Required]
        [MaxLength(255)]
        public required string AccountName { get; set; }

        [Precision(18, 2)]
        public decimal Balance { get; set; }

        [Required]
        [MaxLength(10)]
        public required string Currency { get; set; }

        [MaxLength(255)]
        public string? Notes { get; set; }
        public DateTimeOffset BalanceRecordedDate { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Institution { get; set; }
        public Guid FinancialAccountId { get; set; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset? SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = [];

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
