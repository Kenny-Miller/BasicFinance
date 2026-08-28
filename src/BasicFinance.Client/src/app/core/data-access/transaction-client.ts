import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { IPagedQuery, ISortedQuery, ListResult } from './api-interfaces';

export interface Transaction {
  id: string;
  transactionTypeName: string;
  transactionCategoryName: string;
  accountName: string;
  date: string;
  amount: number;
  description: string;
}

export interface TransactionFilters {
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  accountId?: string;
}

interface ListTransactionsParams extends IPagedQuery, ISortedQuery {
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  accountId?: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionClient {
  client = inject(HttpClient);

  getTransaction(transactionId: string) {
    return this.client.get<Transaction>(`api/transactions/${transactionId}`);
  }

  listTransactions(
    pageSignal: Signal<number>,
    pageSizeSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<TransactionFilters>,
  ) {
    return httpResource<ListResult<Transaction>>(() => {
      const params: ListTransactionsParams = {
        page: pageSignal(),
        pageSize: pageSizeSignal(),
        sortField: sortFieldSignal(),
        sortDirection: sortDirectionSignal(),
        ...filtersSignal(),
      };

      const queryParams = Object.fromEntries(
        Object.entries(params).filter(([_, value]) => value !== undefined),
      );

      return {
        url: 'api/transactions',
        params: queryParams,
      };
    });
  }
}
