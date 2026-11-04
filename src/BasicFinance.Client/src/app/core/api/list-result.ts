export interface ListResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  pageCount: number;
  totalCount: number;
}

export function normalizeListResult<T>(result: {
  items?: T[];
  page?: number;
  pageSize?: number;
  pageCount?: number;
  totalCount?: number;
}): ListResult<T> {
  return {
    items: result.items ?? [],
    page: result.page ?? 1,
    pageSize: result.pageSize ?? 0,
    pageCount: result.pageCount ?? 0,
    totalCount: result.totalCount ?? 0,
  };
}
