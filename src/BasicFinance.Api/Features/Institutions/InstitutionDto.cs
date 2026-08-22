using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Institutions
{
    /// <summary>
    /// Dto containing <see cref="Institution"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Code"></param>
    /// <param name="Name"></param>
    /// <param name="LogoUrl"></param>
    public record InstitutionDto(int Id, string Code, string Name, string? LogoUrl);
}
