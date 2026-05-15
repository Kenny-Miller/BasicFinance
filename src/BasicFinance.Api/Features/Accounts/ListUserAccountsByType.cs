using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the List Recent Accounts Endpoint.
    /// </summary>
    public static class ListUserAccountsByType
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AccountTypeId"></param>
        /// <param name="AccountTypeName"></param>
        /// <param name="DisplayOrder"></param>
        /// <param name="Accounts"></param>
        public record AccountsByTypeDto(Guid AccountTypeId, string AccountTypeName, int DisplayOrder, List<AccountDto> Accounts);

        /// <summary>
        /// Dto containing <see cref="Account"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        public record AccountDto(Guid Id, string Institution, string AccountName, decimal Balance);

        /// <summary>
        /// Lists <see cref="Account"/>s associated with the authenticated user.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="AccountDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts")]
        public static async Task<Ok<ListResult<AccountsByTypeDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var accountsByType = await dbContext.Accounts
               .AsNoTracking()
               .Where(x => x.UserId == user.Id)
               .Where(x => x.IsActive)
               .GroupBy(x => x.AccountType)
               .ToListAsync(cancellationToken);

            var mapped = accountsByType
                .OrderBy(group => group.Key.DisplayOrder)
                .Select(group => new AccountsByTypeDto(
                 group.Key.AccountTypeId,
                 group.Key.AccountTypeName,
                 [.. group.Select(account => new AccountDto(
                     account.AccountId,
                     account.Institution,
                     account.AccountName,
                     account.Balance))
                    .OrderBy(x => x.AccountName)]))
                .OrderBy(x => x.d)
                .ToList();

            return TypedResults.Ok(new ListResult<AccountsByTypeDto>(mapped, 1, mapped.Count, mapped.Count));
        }
    }
}
