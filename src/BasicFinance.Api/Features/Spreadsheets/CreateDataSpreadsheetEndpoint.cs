using BasicFinance.Api.ParameterStrategy;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public record Response();

    public static class CreateSpreadsheetEndpoint
    {


        [Authorize]
        [WolverinePost("api/spreadsheets")]
        public static async Task<Results<Created, BadRequest>> HandleAsync(Request request, HttpContext context)
        {
            //var spreadsheet = await spreadsheetClient.GetSpreadsheetAsync(request.SpreadsheetId);
            //if (spreadsheet == null)
            //{
            //    return TypedResults.NotFound();
            //}
            var user = context.MapToUser();



            return TypedResults.Created();
        }
    }
}
