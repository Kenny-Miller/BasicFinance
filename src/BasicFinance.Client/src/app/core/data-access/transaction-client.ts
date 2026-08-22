import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { ListResult } from '../../shared/api/list-result';
import { IPagedQuery, ISortedQuery } from './api-interfaces';

export interface Transaction {
  id: string;
  code: string;
  name: string;
  logoUrl: string;
}

export interface TransactionFilters {
  startDate?: Date;
  endDate?: Date;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  accountId?: string;
}

interface ListTransactionsParams extends IPagedQuery, ISortedQuery {
  startDate?: Date;
  endDate?: Date;
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
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<TransactionFilters>,
  ) {
    return httpResource<ListResult<Transaction>>(() => {
      const params: ListTransactionsParams = {
        page: pageSignal(),
        pageSize: 20,
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
