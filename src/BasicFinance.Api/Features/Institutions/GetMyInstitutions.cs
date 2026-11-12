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
        /// Response for the <see cref="GetMyInstitutions"/> endpoint.
        /// </summary>
        /// <param name="InstitutionId">The institution identifier.</param>
        /// <param name="InstitutionCode">The institution code.</param>
        /// <param name="Name">The institution name.</param>
        /// <param name="LogoUrl">The URL of the institution's logo.</param>
        /// <param name="ActiveAccounts">The number of active accounts.</param>
        /// <param name="TotalBalance">The total current balance across the active accounts.</param>
        public record GetMyInstitutionsResponse(int InstitutionId, string InstitutionCode, string Name, string? LogoUrl, int ActiveAccounts, decimal TotalBalance);


        /// <summary>
        /// Retrieves distinct institutions for the authenticated user.
        /// Only includes institutions that have at least one active account belonging to the user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="GetMyInstitutionsResponse"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/my/institutions")]
        public static async Task<Ok<List<GetMyInstitutionsResponse>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var institutions = await dbContext.Institutions
                .AsNoTracking()
                .Where(i => i.IsActive)
                .Where(i => i.Accounts.Any(a => a.UserId == user.Id && a.IsActive))
                .Select(i => new GetMyInstitutionsResponse(
                    i.InstitutionId,
                    i.InstitutionCode,
                    i.Name,
                    i.LogoUrl,
                    i.Accounts.Count(a => a.UserId == user.Id && a.IsActive),
                    i.Accounts
                        .Where(a => a.UserId == user.Id && a.IsActive)
                        .Sum(a => a.Ledger.OrderByDescending(l => l.BalanceRecordedDate).First().Balance)))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(institutions);
        }
    }
}
