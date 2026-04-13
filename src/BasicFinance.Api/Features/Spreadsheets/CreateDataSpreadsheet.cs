using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spreadsheets
{
    /// <summary>
    /// The <see cref="CreateDataSpreadsheet"/> class contains
    /// all logic associated with the Create Data Spreadsheet Endpoint.
    /// </summary>
    public static class CreateDataSpreadsheet
    {
        /// <summary>
        /// Request Dto for the Create Data Spreadsheet endpoint.
        /// </summary>
        /// <param name="GoogleSpreadsheetId">The Google Spreadsheet identifier to associate with the authenticated user.</param>
        public record Request(string GoogleSpreadsheetId);

        /// <summary>
        /// Validator for <see cref="Request"/>.
        /// </summary>
        public class RequestValidator : AbstractValidator<Request>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RequestValidator"/> class.
            /// </summary>
            public RequestValidator()
            {
                RuleFor(x => x.GoogleSpreadsheetId).NotEmpty().WithMessage("SpreadsheetId is required.");
            }
        }

        /// <summary>
        /// Creates a new <see cref="DataSpreadsheet"/> for the authenticated user and the specified Google Spreadsheet.
        /// </summary>
        /// <param name="googleApiToken">Google API token supplied via the <c>x-google-auth-token</c> header.</param>
        /// <param name="request">Request body containing the target Google Spreadsheet Id.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="googleUserClient">Client used to call Google APIs on behalf of the user.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> for persisting the entity.</param>
        /// <param name="bus">Message bus used to publish synchronization commands.</param>
        /// <returns>
        /// Returns <see cref="Created"/> when a new <see cref="DataSpreadsheet"/> was created or already exists,
        /// or <see cref="BadRequest"/> when the supplied Google Spreadsheet could not be retrieved.
        /// </returns>
        [Authorize]
        [WolverinePost("api/spreadsheets")]
        public static async Task<Results<Created, BadRequest>> HandleAsync(
            [FromHeader(Name = "x-google-auth-token")] string googleApiToken,
            Request request,
            AuthenticatedUser user,
            GoogleUserClient googleUserClient,
            AppDbContext dbContext,
            IMessageBus bus)
        {
            var googleSpreadsheet = await googleUserClient.GetSpreadsheetAsync(request.GoogleSpreadsheetId, googleApiToken);
            if (googleSpreadsheet == null)
            {
                return TypedResults.BadRequest();
            }

            var dataSpreadsheet = await dbContext.DataSpreadsheets
                .AsNoTracking()
                .SingleOrDefaultAsync(s =>
                    s.GoogleSheetId == request.GoogleSpreadsheetId &&
                    s.UserId == user.Id &&
                    s.IsActive);

            if (dataSpreadsheet != null)
            {
                return TypedResults.Created();
            }

            var dataSpreadsheetEntity = new DataSpreadsheet
            {
                GoogleSheetId = request.GoogleSpreadsheetId,
                GoogleSheetName = googleSpreadsheet.Properties.Title,
                UserId = user.Id,
                SystemCreatedDate = DateTime.UtcNow,
                SystemModifiedDate = DateTime.UtcNow,
                IsActive = true
            };

            dbContext.DataSpreadsheets.Add(dataSpreadsheetEntity);
            await googleUserClient.GrantSpreadSheetAccessAsync(request.GoogleSpreadsheetId, googleApiToken);
            await dbContext.SaveChangesAsync();
            await bus.PublishAsync(new SyncFinancialData(user.Id));

            return TypedResults.Created();
        }
    }
}
