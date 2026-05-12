namespace BasicFinance.Domain.Queries
{
    public interface IPagedQuery
    {
        /// <summary>
        /// Gets a value indicating the current page of the paged query.
        /// </summary>
        public int? Page { get; }

        /// <summary>
        /// Gets a value indicating the page size of the paged query.
        /// </summary>
        public int? PageSize { get; }

    }
}
