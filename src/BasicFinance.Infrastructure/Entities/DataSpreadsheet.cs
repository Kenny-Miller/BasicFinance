using System.ComponentModel.DataAnnotations;

namespace BasicFinance.Infrastructure.Entities
{
    public class DataSpreadsheet : IEntity
    {
        [Key]
        public Guid Id { get; init; }

        [Required]
        [MaxLength(50)]
        public required string GoogleSheetId { get; init; }

        [Required]
        [MaxLength(36)]
        public required string UserId { get; init; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
