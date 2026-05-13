namespace BasicFinance.Domain.Queries
{
    public interface ISortedQuery
    {
        /// <summary>
        /// Gets a value indicating the field that the query should be sorted by.
        /// </summary>
        public string? SortField { get; }

        /// <summary>
        /// Gets a value indicating the direction that the query should be sorted in (e.g. "asc" or "desc").
        /// </summary>
        public string? SortDirection { get; }
    }
}
