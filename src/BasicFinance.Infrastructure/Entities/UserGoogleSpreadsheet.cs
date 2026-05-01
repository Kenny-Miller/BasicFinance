using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicFinance.Infrastructure.Entities
{
    public class UserGoogleSpreadsheet : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user's associated Google Spreadsheet.
        /// </summary>
        [Key]
        public Guid UserGoogleSpreadsheetId { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public Guid Id => UserGoogleSpreadsheetId;

        /// <summary>
        /// Gets the unique identifier of the associated Google Sheet.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string GoogleSheetId { get; init; }

        /// <summary>
        /// Name of the Google Spreadsheet.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string GoogleSheetName { get; set; }

        /// <summary>
        /// The last time the spreadsheet was successfully synced.
        /// </summary>
        public DateTimeOffset? LastSyncedDate { get; set; }

        /// <summary>
        /// UserId of the owner of the spreadsheet.
        /// </summary>
        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; }

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; }

        /// <summary>
        /// Navigation collection for accounts associated with this spreadsheet.
        /// </summary>
        public ICollection<Account> Accounts { get; set; } = [];
    }
}
