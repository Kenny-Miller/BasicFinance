using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using AccountType = BasicFinance.Infrastructure.Entities.AccountType;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the Get Accounts By Type Endpoint.
    /// </summary>
    public static class GetUserAccountsByType
    {
        /// <summary>
        /// Dto containing a list of <see cref="Account"/>s grouped by <see cref="AccountType"/>.
        /// </summary>
        /// <param name="AccountTypeCode"></param>
        /// <param name="TotalBalance"></param>
        /// <param name="Accounts"></param>
        public record AccountsByTypeDto(string AccountTypeCode, decimal TotalBalance, List<AccountDto> Accounts);

        /// <summary>
        /// Dto containing <see cref="Account"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        public record AccountDto(Guid Id, string Institution, string AccountName, decimal Balance);

        /// <summary>
        /// Lists <see cref="Account"/> grouped by thier <see cref="AccountType"/> associated with the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="AccountDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/byType")]
        public static async Task<Ok<ListResult<AccountsByTypeDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var userAccounts = dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            var joinedResults = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .GroupJoin(
                    userAccounts,
                    accountType => accountType.AccountTypeId,
                    account => account.AccountTypeId,
                    (accountType, accounts) => new
                    {
                        accountType.AccountTypeCode,
                        Accounts = accounts
                    })
                .ToListAsync(cancellationToken);

            var mapped = joinedResults
                .Select(x => new AccountsByTypeDto(
                    x.AccountTypeCode,
                    x.Accounts.Sum(a => a.Balance),
                    [.. x.Accounts.OrderBy(a => a.AccountName)
                        .Select(a => new AccountDto(
                            a.AccountId,
                            a.Institution,
                            a.AccountName,
                            a.Balance))]))
                .ToList();

            return TypedResults.Ok(new ListResult<AccountsByTypeDto>(mapped, 1, mapped.Count, mapped.Count));
        }
    }
}