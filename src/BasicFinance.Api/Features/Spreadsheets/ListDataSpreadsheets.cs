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
    /// Contains all logic associated with the List Data Spreadsheets endpoint.
    /// </summary>
    public static class ListDataSpreadSheets
    {
        /// <summary>
        /// Lists active Google data spreadsheets associated with the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="DataSpreadsheet"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/spreadsheets")]
        public static async Task<Results<Ok<List<DataSpreadsheet>>, BadRequest>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext)
        {
            var dataSpreadsheets = await dbContext.DataSpreadsheets
                .AsNoTracking()
                .Where(s =>
                    s.UserId == user.Id &&
                    s.IsActive)
                .ToListAsync();

            return TypedResults.Ok(dataSpreadsheets);
        }
    }
}
