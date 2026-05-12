using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spreadsheets
{
    /// <summary>
    /// Contains all logic associated with the List User Google Spreadsheets endpoint.
    /// </summary>
    public static class ListUserGoogleSpreadsheets
    {
        /// <summary>
        /// Dto containing <see cref="UserGoogleSpreadsheet"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="GoogleSheetId"></param>
        /// <param name="GoogleSheetName"></param>
        /// <param name="CreatedDate"></param>
        public record UserGoogleSpreadSheetDto(Guid Id, string GoogleSheetId, string GoogleSheetName, DateTimeOffset CreatedDate);

        /// <summary>
        /// Lists <see cref="UserGoogleSpreadsheet"/>s associated with the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="UserGoogleSpreadsheet"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/spreadsheets")]
        public static async Task<Ok<ListResult<UserGoogleSpreadSheetDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var baseQuery = dbContext.UserGoogleSpreadsheets
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var userSpreadSheets = await baseQuery
                .OrderBy(x => x.GoogleSheetName)
                    .ThenBy(x => x.UserGoogleSpreadsheetId)
                .Select(x => new UserGoogleSpreadSheetDto(x.UserGoogleSpreadsheetId, x.GoogleSheetId, x.GoogleSheetName, x.SystemCreatedDate))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<UserGoogleSpreadSheetDto>(userSpreadSheets, 1, totalCount, totalCount));
        }
    }
}