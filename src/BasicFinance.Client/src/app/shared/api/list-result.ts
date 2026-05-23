export type ListResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  pageCount: number;
  totalCount: number;
};
