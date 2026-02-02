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
    /// Contains all logic associated with the Delete Data Spreadsheet endpoint.
    /// </summary>
    public static class DeleteDataSpreadsheet
    {
        /// <summary>
        /// Soft-deletes (deactivates) a data spreadsheet owned by the authenticated user.
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
            var dataSpreadsheet = await dbContext.DataSpreadsheets.SingleOrDefaultAsync(s =>
                s.Id == spreadsheetId &&
                s.UserId == user.Id &&
                s.IsActive);

            if (dataSpreadsheet == null)
            {
                return TypedResults.BadRequest();
            }

            dataSpreadsheet.IsActive = false;
            dataSpreadsheet.SystemModifiedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            return TypedResults.Ok();
        }
    }
}
