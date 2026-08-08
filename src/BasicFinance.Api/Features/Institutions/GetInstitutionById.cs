using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Institutions;

/// <summary>
/// Contains all logic associated with the <see cref="GetInstitutionById"/> Endpoint.
/// </summary>
public class GetInstitutionById
{
    /// <summary>
    /// Dto containing <see cref="Institution"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="InstitutionCode"></param>
    /// <param name="Name"></param>
    /// <param name="LogoUrl"></param>
    public record InstitutionDto(
        int Id,
        string InstitutionCode,
        string Name,
        string? LogoUrl);

    /// <summary>
    /// Gets an <see cref="Institution"/> with the specified Id.
    /// </summary>
    /// <param name="institutionId">The institution identifier.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted institutions.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns <see cref="Ok{TValue}"/> when successful,
    /// or <see cref="BadRequest"/> on failure.
    /// </returns>
    [Authorize]
    [WolverineGet("api/institutions/{institutionId:int}")]
    public static async Task<Results<Ok<InstitutionDto>, BadRequest<string>>> HandleAsync(
        [FromRoute] int institutionId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var institution = await dbContext.Institutions
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .Where(x => x.IsActive)
            .Select(x => new InstitutionDto(
                x.InstitutionId,
                x.InstitutionCode,
                x.Name,
                x.LogoUrl))
            .SingleOrDefaultAsync(cancellationToken);

        return institution != null
            ? TypedResults.Ok(institution)
            : TypedResults.BadRequest("Institution with the specified Id was not found");
    }
}
