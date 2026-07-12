import { httpResource } from '@angular/common/http';
import { inject, Injectable, Signal } from '@angular/core';
import { ListResult } from '../../../shared/api/list-result';
import { Transaction } from '../../../shared/api/transactions/transactions';

@Injectable({
  providedIn: 'root',
})
export class TransactionsClient {
  createResource(
    pageSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
  ) {
    return httpResource<ListResult<Transaction>>(() => ({
      url: 'api/transactions',
      params: {
        page: pageSignal(),
        pageSize: 20,
        sortField: sortFieldSignal(),
        sortDirection: sortDirectionSignal(),
      },
    }));
  }
}
