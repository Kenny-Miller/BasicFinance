export interface IPagedQuery {
  page?: number;
  pageSize?: number;
}

export interface ISortedQuery {
  sortField?: string;
  sortDirection?: string;
}

export interface ListResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  pageCount: number;
  totalCount: number;
}
