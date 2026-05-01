using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spreadsheets
{
    /// <summary>
    /// Contains all logic associated with the Delete User Google Spreadsheet endpoint.
    /// </summary>
    public static class DeleteDataSpreadsheet
    {
        /// <summary>
        /// Soft-deletes (deactivates) a user google spreadsheet owned by the authenticated user.
        /// </summary>
        /// <param name="spreadsheetId">The identifier of the data spreadsheet to delete.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to update the entity.</param>
        /// <returns>
        /// Returns <see cref="Ok"/> when the spreadsheet was successfully deactivated,
        /// or <see cref="BadRequest"/> when the specified spreadsheet does not exist or is not accessible.
        /// </returns>
        [Authorize]
        [WolverineDelete("api/spreadsheets/{spreadsheetId:guid}")]
        public static async Task<Results<Ok, BadRequest>> HandleAsync(
            Guid spreadsheetId,
           [NotBody] AuthenticatedUser user,
           [FromServices] AppDbContext dbContext)
        {
            var sheet = await dbContext.UserGoogleSpreadsheets.SingleOrDefaultAsync(s =>
                s.UserGoogleSpreadsheetId == spreadsheetId &&
                s.UserId == user.Id &&
                s.IsActive);

            if (sheet == null)
            {
                return TypedResults.BadRequest();
            }

            sheet.IsActive = false;
            sheet.SystemModifiedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            return TypedResults.Ok();
        }
    }
}
