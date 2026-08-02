using BasicFinance.Domain.Queries;

namespace BasicFinance.Infrastructure.UnitTests.Helpers;

internal sealed class MockPagedQuery : IPagedQuery
{
    public int? Page { get; }
    public int? PageSize { get; }

    public MockPagedQuery(int? page, int? pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }
}