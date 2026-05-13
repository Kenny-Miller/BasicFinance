using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spreadsheets
{
    /// <summary>
    /// The <see cref="GetUserGoogleSpreadsheet"/> class contains
    /// all logic associated with the Get User Google Spreadsheet Endpoint.
    /// </summary>
    public static class GetUserGoogleSpreadsheet
    {
        /// <summary>
        /// Response Dto for the <see cref="GetUserGoogleSpreadsheet"/> endpoint.
        /// </summary>
        /// <param name="UserGoogleSpreadsheetId"></param>
        /// <param name="UserId"></param>
        /// <param name="GoogleSpreadsheetId"></param>
        /// <param name="GoogleSpreadsheetName"></param>
        /// <param name="CreatedDate"></param>
        public record Response(Guid UserGoogleSpreadsheetId, string UserId, string GoogleSpreadsheetId, string GoogleSpreadsheetName, DateTimeOffset CreatedDate);

        /// <summary>
        /// Gets a <see cref="UserGoogleSpreadsheet"/> for the authenticated user and the specified Google Spreadsheet.
        /// </summary>
        /// <param name="userGoogleSpreadsheetId">Id of the UserGoogleSpreadsheet to retrieve.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> for persisting the entity.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok"/> when the specified <see cref="UserGoogleSpreadsheet"/> exists,
        /// or <see cref="NotFound"/> when the specified <see cref="UserGoogleSpreadsheet"/> could not be retrieved.
        /// </returns>
        [Authorize]
        [WolverineGet("api/spreadsheets/{userGoogleSpreadsheetId:guid}")]
        public static async Task<Results<Ok<Response>, NotFound<string>>> HandleAsync(
            Guid userGoogleSpreadsheetId,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var userGoogleSpreadsheet = await dbContext.UserGoogleSpreadsheets
                .AsNoTracking()
                .SingleOrDefaultAsync(s =>
                    s.UserGoogleSpreadsheetId == userGoogleSpreadsheetId &&
                    s.UserId == user.Id &&
                    s.IsActive,
                    cancellationToken);

            if (userGoogleSpreadsheet == null)
            {
                return TypedResults.NotFound("The specified spreadsheet does not exist or is not accessible.");
            }

            var response = new Response(
                userGoogleSpreadsheet.UserGoogleSpreadsheetId,
                userGoogleSpreadsheet.UserId,
                userGoogleSpreadsheet.GoogleSheetId,
                userGoogleSpreadsheet.GoogleSheetName,
                userGoogleSpreadsheet.SystemCreatedDate);

            return TypedResults.Ok(response);
        }
    }
}
