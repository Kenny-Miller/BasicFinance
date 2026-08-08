using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BasicFinance.Infrastructure.Entities
{
    public class Institution : IEntity
    {
        [Key]
        public int InstitutionId { get; set; }

        [Required]
        [MaxLength(25)]
        public required string InstitutionCode { get; init; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; init; }

        [MaxLength(500)]
        public string? LogoUrl { get; init; }

        public ICollection<Account> Accounts { get; set; } = [];

        public DateTimeOffset SystemCreatedDate { get; init; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? SystemModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [SetsRequiredMembers]
        public Institution(string institutionCode, string name, string? logoUrl)
        {
            InstitutionCode = institutionCode;
            Name = name;
            LogoUrl = logoUrl;
        }

        private Institution()
        {
        }
    }
}