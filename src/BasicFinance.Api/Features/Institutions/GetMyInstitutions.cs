using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Institutions
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetMyInstitutions"/> Endpoint.
    /// </summary>
    public static class GetMyInstitutions
    {
        /// <summary>
        /// Dto representing an institution.
        /// </summary>
        /// <param name="InstitutionId"></param>
        /// <param name="Name"></param>
        public record InstitutionDto(Guid InstitutionId, string Name);

        /// <summary>
        /// Retrieves distinct institutions for the authenticated user.
        /// Only includes institutions that have at least one active account belonging to the user.
        /// </summary>s
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="InstitutionDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/my/institutions")]
        public static async Task<Ok<List<InstitutionDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var institutions = await dbContext.Institutions
                .AsNoTracking()
                .Where(i => i.IsActive)
                .Where(i => i.Accounts.Any(a => a.UserId == user.Id && a.IsActive))
                .Select(i => new InstitutionDto(i.InstitutionId, i.Name))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(institutions);
        }
    }
}
