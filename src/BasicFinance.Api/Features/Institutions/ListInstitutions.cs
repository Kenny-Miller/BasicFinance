using System.Collections.Frozen;
using System.Linq.Expressions;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Institutions
{
    /// <summary>
    /// Contains all logic associated with the <see cref="ListInstitutions"/> Endpoint.
    /// </summary>
    public static class ListInstitutions
    {
        /// <summary>
        /// Request Dto for the <see cref="ListInstitutions"/> endpoint.
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="PageSize"></param>
        /// <param name="SortField"></param>
        /// <param name="SortDirection"></param>
        public record Request(
            int? Page,
            int? PageSize,
            string? SortField,
            string? SortDirection) : IPagedQuery, ISortedQuery;

        /// <summary>
        /// Dto containing <see cref="Institution"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="InstitutionCode"></param>
        /// <param name="Name"></param>
        /// <param name="LogoUrl"></param>
        public record InstitutionDto(
            Guid Id,
            string InstitutionCode,
            string Name,
            string? LogoUrl);

        /// <summary>
        /// Retrieves active <see cref="Institution"/>s based on the provided search criteria.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted institutions.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a <see cref="ListResult{TValue}"/> of <see cref="InstitutionDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/institutions/")]
        public static async Task<Ok<ListResult<InstitutionDto>>> HandleAsync(
            [FromQuery] Request request,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var sortField = request.SortField ?? nameof(InstitutionDto.Name);
            var sortExpressionSelector = SortFieldExpressionSelectors.GetValueOrDefault(sortField, x => x.Name);

            var baseQuery = dbContext.Institutions
                .AsNoTracking()
                .Where(x => x.IsActive);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var institutions = await baseQuery
                .OrderBy(sortExpressionSelector, request)
                    .ThenBy(x => x.Name, request)
                .Paginate(request)
                .Select(x => new InstitutionDto(
                    x.InstitutionId,
                    x.InstitutionCode,
                    x.Name,
                    x.LogoUrl))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<InstitutionDto>(institutions, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="Institution"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<Institution, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<Institution, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(InstitutionDto.Id)] = x => x.InstitutionId,
            [nameof(InstitutionDto.InstitutionCode)] = x => x.InstitutionCode,
            [nameof(InstitutionDto.Name)] = x => x.Name,
        }.ToFrozenDictionary();
    }
}
