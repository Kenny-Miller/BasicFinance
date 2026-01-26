using BasicFinance.Api.Extensions;
using BasicFinance.Domain.Models;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spreadsheets
{
    public record Request(string SpreadsheetId);
    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.SpreadsheetId).NotEmpty().WithMessage("SpreadsheetId is required.");
        }
    }

    public static class CreateSpreadsheetEndpoint
    {
        public static UserAuth Load(HttpContext httpContext)
        {
            return httpContext.MapToUser();
        }

        [Authorize]
        [WolverinePost("api/spreadsheets")]
        public static async Task<Results<Created, NotFound>> HandleAsync(
            [FromHeader(Name = "x-google-auth-token")] string googleApiToken,
            Request request,
            UserAuth user,
            GoogleUserClient spreadsheetClient,
            AppDbContext dbContext)
        {
            var googleSpreadsheet = await spreadsheetClient.GetSpreadsheetAsync(request.SpreadsheetId, googleApiToken);
            if (googleSpreadsheet == null)
            {
                return TypedResults.NotFound();
            }

            var dataSpreadsheet = await dbContext.DataSpreadsheets.SingleOrDefaultAsync(s =>
                s.GoogleSheetId == request.SpreadsheetId &&
                s.UserId == user.Id &&
                s.IsActive);

            if (dataSpreadsheet != null)
            {
                return TypedResults.Created();
            }

            var dataSpreadsheetEntity = new DataSpreadsheet
            {
                GoogleSheetId = request.SpreadsheetId,
                UserId = user.Id,
                SystemCreatedDate = DateTime.UtcNow,
                SystemModifiedDate = DateTime.UtcNow,
                IsActive = true
            };

            dbContext.DataSpreadsheets.Add(dataSpreadsheetEntity);

            await spreadsheetClient.GrantSpreadSheetAccessAsync(request.SpreadsheetId, googleApiToken);
            await dbContext.SaveChangesAsync();

            return TypedResults.Created();
        }
    }
}
