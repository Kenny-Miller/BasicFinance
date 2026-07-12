import { httpResource } from '@angular/common/http';
import { Injectable, Signal } from '@angular/core';
import { ListResult } from '../../../shared/api/list-result';
import { Transaction } from '../../../shared/api/transactions/transactions';

export interface TransactionFilters {
  startDate: string;
  endDate: string;
  minAmount: string;
  maxAmount: string;
  transactionTypeId: string;
  transactionCategoryId: string;
  search: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionsClient {
  createResource(
    pageSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<TransactionFilters>,
  ) {
    return httpResource<ListResult<Transaction>>(() => {
      const f = filtersSignal();
      const params = new URLSearchParams();
      params.set('page', String(pageSignal()));
      params.set('pageSize', '20');
      params.set('sortField', sortFieldSignal());
      params.set('sortDirection', sortDirectionSignal());
      if (f.startDate) params.set('startDate', f.startDate);
      if (f.endDate) params.set('endDate', f.endDate);
      if (f.minAmount) params.set('minAmount', f.minAmount);
      if (f.maxAmount) params.set('maxAmount', f.maxAmount);
      if (f.transactionTypeId) params.set('transactionTypeId', f.transactionTypeId);
      if (f.transactionCategoryId) params.set('transactionCategoryId', f.transactionCategoryId);
      if (f.search) params.set('search', f.search);
      const qs = params.toString();
      return qs ? `api/transactions?${qs}` : 'api/transactions';
    });
  }
}
