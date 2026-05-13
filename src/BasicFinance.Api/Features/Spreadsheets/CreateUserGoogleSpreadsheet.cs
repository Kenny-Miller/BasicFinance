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
    /// The <see cref="CreateUserGoogleSpreadsheet"/> class contains
    /// all logic associated with the Create User Google Spreadsheet Endpoint.
    /// </summary>
    public static class CreateUserGoogleSpreadsheet
    {
        /// <summary>
        /// Request Dto for the <see cref="CreateUserGoogleSpreadsheet"/> endpoint.
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
                RuleFor(x => x.GoogleSpreadsheetId).NotEmpty().WithMessage("GoogleSpreadsheetId is required.");
            }
        }

        /// <summary>
        /// Response Dto for the <see cref="CreateUserGoogleSpreadsheet"/> endpoint.
        /// </summary>
        /// <param name="UserGoogleSpreadsheetId"></param>
        /// <param name="UserId"></param>
        /// <param name="GoogleSpreadsheetId"></param>
        /// <param name="GoogleSpreadsheetName"></param>
        /// <param name="CreatedDate"></param>
        public record Response(Guid UserGoogleSpreadsheetId, string UserId, string GoogleSpreadsheetId, string GoogleSpreadsheetName, DateTimeOffset CreatedDate);

        /// <summary>
        /// Creates a new <see cref="UserGoogleSpreadsheet"/> for the authenticated user and the specified Google Spreadsheet.
        /// </summary>
        /// <param name="googleApiToken">Google API token supplied via the <c>x-google-auth-token</c> header.</param>
        /// <param name="request">Request body containing the target Google Spreadsheet Id.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="googleUserClient">Client used to call Google APIs on behalf of the user.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> for persisting the entity.</param>
        /// <param name="bus">Message bus used to publish synchronization commands.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Created"/> when a new <see cref="UserGoogleSpreadsheet"/> was created,
        /// a <see cref="NotFound"/> when the specified Google Spreadsheet could not be found or accessed, or
        /// a <see cref="Conflict"/> when the specified Google Spreadsheet is already associated with the authenticated user.
        /// </returns>
        [Authorize]
        [WolverinePost("api/spreadsheets")]
        public static async Task<Results<Created<Response>, NotFound<string>, Conflict<string>>> HandleAsync(
            [FromHeader(Name = "x-google-auth-token")] string googleApiToken,
            Request request,
            AuthenticatedUser user,
            GoogleUserClient googleUserClient,
            AppDbContext dbContext,
            IMessageBus bus,
            CancellationToken cancellationToken = default)
        {
            var googleSpreadsheet = await googleUserClient.GetSpreadsheetAsync(request.GoogleSpreadsheetId, googleApiToken, cancellationToken);
            if (googleSpreadsheet == null)
            {
                return TypedResults.NotFound("The specified Google Spreadsheet could not be found or accessed.");
            }

            var userGoogleSpreadSheet = await dbContext.UserGoogleSpreadsheets
                .AsNoTracking()
                .SingleOrDefaultAsync(s =>
                    s.GoogleSheetId == request.GoogleSpreadsheetId &&
                    s.UserId == user.Id &&
                    s.IsActive,
                    cancellationToken);

            if (userGoogleSpreadSheet != null)
            {
                return TypedResults.Conflict("The specified Google Spreadsheet is already associated with the authenticated user.");
            }

            var userGoogleSpreadsheet = new UserGoogleSpreadsheet(user.Id, request.GoogleSpreadsheetId, googleSpreadsheet.Properties.Title);
            dbContext.UserGoogleSpreadsheets.Add(userGoogleSpreadsheet);
            await googleUserClient.GrantSpreadSheetAccessAsync(request.GoogleSpreadsheetId, googleApiToken, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await bus.PublishAsync(new SyncFinancialData(userGoogleSpreadsheet.UserGoogleSpreadsheetId));

            var response = new Response(
                userGoogleSpreadsheet.UserGoogleSpreadsheetId,
                userGoogleSpreadsheet.UserId,
                userGoogleSpreadsheet.GoogleSheetId,
                userGoogleSpreadsheet.GoogleSheetName,
                userGoogleSpreadsheet.SystemCreatedDate);

            return TypedResults.Created($"api/spreadsheets/{userGoogleSpreadsheet.UserGoogleSpreadsheetId}", response);
        }
    }
}
