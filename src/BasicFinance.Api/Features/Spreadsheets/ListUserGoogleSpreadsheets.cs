using System.Collections.Frozen;
using System.Linq.Expressions;
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
        /// Request Dto for the <see cref="ListUserGoogleSpreadsheets"/> endpoint.
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="PageSize"></param>
        /// <param name="SortField"></param>
        /// <param name="SortDirection"></param>
        public record Request(int Page, int PageSize, string? SortField, string? SortDirection) : IPagedQuery, ISortedQuery;

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
                RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
                RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("PageSize must be greater than 0.");
            }
        }

        /// <summary>
        /// Dto containing <see cref="UserGoogleSpreadsheet"/> data.
        /// </summary>
        /// <param name="UserGoogleSpreadsheetId"></param>
        /// <param name="GoogleSpreadsheetId"></param>
        /// <param name="GoogleSpreadsheetName"></param>
        /// <param name="CreatedDate"></param>
        public record UserGoogleSpreadSheetDto(Guid UserGoogleSpreadsheetId, string GoogleSpreadsheetId, string GoogleSpreadsheetName, DateTimeOffset CreatedDate);

        /// <summary>
        /// Lists <see cref="UserGoogleSpreadsheet"/>s associated with the authenticated user.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="UserGoogleSpreadsheet"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/spreadsheets")]
        public static async Task<Ok<PagedResponse<UserGoogleSpreadSheetDto>>> HandleAsync(
            Request request,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var sortField = request.SortField ?? nameof(UserGoogleSpreadSheetDto.CreatedDate);
            var sortExpressionSelector = SortFieldExpressionSelectors.GetValueOrDefault(sortField, x => x.CreatedDate);

            var baseQuery = dbContext.UserGoogleSpreadsheets
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var userSpreadSheets = await baseQuery
                .Select(x => new UserGoogleSpreadSheetDto(x.UserGoogleSpreadsheetId, x.GoogleSheetId, x.GoogleSheetName, x.SystemCreatedDate))
                .OrderBy(sortExpressionSelector, request)
                    .ThenBy(x => x.UserGoogleSpreadsheetId, request)
                .Paginate(request)
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new PagedResponse<UserGoogleSpreadSheetDto>(userSpreadSheets, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="UserGoogleSpreadSheetDto"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<UserGoogleSpreadSheetDto, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<UserGoogleSpreadSheetDto, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(UserGoogleSpreadSheetDto.UserGoogleSpreadsheetId)] = x => x.UserGoogleSpreadsheetId,
            [nameof(UserGoogleSpreadSheetDto.GoogleSpreadsheetId)] = x => x.GoogleSpreadsheetId,
            [nameof(UserGoogleSpreadSheetDto.GoogleSpreadsheetName)] = x => x.GoogleSpreadsheetName,
            [nameof(UserGoogleSpreadSheetDto.CreatedDate)] = x => x.CreatedDate,
        }.ToFrozenDictionary();
    }
}