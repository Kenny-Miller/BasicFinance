namespace BasicFinance.Domain.Queries
{
    /// <summary>
    /// The <see cref="ListResult{T}"/> interface represents a standardized paged response for a query containing a collection of items of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the paged response.</typeparam>
    public record ListResult<T>
    {
        /// <summary>
        /// Gets a value indicating the items returned in the paged query response.
        /// </summary>
        public IEnumerable<T> Items { get; init; } = [];

        /// <summary>
        /// Gets a value indicating the current page of the paged query response.
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// Gets a value indicating the page size used of the paged query response.
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        /// Gets a value indicating the total number of pages available in the paged query response.
        /// </summary>
        public int PageCount => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

        /// <summary>
        /// Gets a value indicating the total number of items available in the paged query response.
        /// </summary>
        public int TotalCount { get; init; }

        /// Constructs a new instance of the <see cref="ListResult{T}"/> class.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalCount"></param>
        public ListResult(IEnumerable<T> items, int? page, int? pageSize, int? totalCount)
        {
            this.Items = items;
            this.Page = !page.HasValue || page <= QueryConstants.DefaultPage ? QueryConstants.DefaultPage : page.Value;
            this.PageSize = !pageSize.HasValue || pageSize <= 0 ? QueryConstants.DefaultPageSize : pageSize.Value;
            this.TotalCount = !totalCount.HasValue || totalCount < 0 ? 0 : totalCount.Value;
        }
    }
}