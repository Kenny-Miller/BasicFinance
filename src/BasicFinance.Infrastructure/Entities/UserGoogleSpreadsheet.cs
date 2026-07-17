using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BasicFinance.Infrastructure.Entities
{
    public class UserGoogleSpreadsheet : IEntity
    {
        /// <summary>
        /// Gets or sets a value indicating the unique identifier of the entity.
        /// </summary>
        [Key]
        public Guid UserGoogleSpreadsheetId { get; set; }

        /// <inheritdoc />
        [NotMapped]
        public Guid Id => UserGoogleSpreadsheetId;

        /// <summary>
        /// Gets a value indicating the unique identifier of the associated Google Sheet.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string GoogleSheetId { get; init; }

        /// <summary>
        /// Gets or sets a value indicating the name of the Google Spreadsheet.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string GoogleSheetName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the last time the spreadsheet was successfully synced.
        /// </summary>
        public DateTimeOffset? LastSyncedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the owner of the spreadsheet.
        /// </summary>
        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }

        /// <inheritdoc />
        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <inheritdoc />
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation collection of <see cref="Account"/> entries associated with this spreadsheet.
        /// </summary>
        public ICollection<Account> Accounts { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="UserGoogleSpreadsheet"/> class for use by Entity Framework.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="googleSheetId"></param>
        /// <param name="googleSheetName"></param>
        [SetsRequiredMembers]
        public UserGoogleSpreadsheet(string userId, string googleSheetId, string googleSheetName)
        {
            UserId = userId;
            GoogleSheetId = googleSheetId;
            GoogleSheetName = googleSheetName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserGoogleSpreadsheet"/> class for use by Entity Framework.
        /// </summary>
        private UserGoogleSpreadsheet()
        {
            // For EF
        }
    }
}