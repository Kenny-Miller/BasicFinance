import { HttpClient, HttpParams, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { ListResult } from '../../shared/api/list-result';
import { IPagedQuery, ISortedQuery } from './api-interfaces';

export interface Account {
  id: string;
  name: string;
  accountTypeCode: string;
  institution: string;
  balance: number;
  balanceRecordedDate: Date;
}

export interface AccountFilters {
  accountTypeCode?: string;
  institution?: string;
}

interface ListAccountsParams extends IPagedQuery, ISortedQuery {
  accountTypeCode?: string;
  institution?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AccountClient {
  client = inject(HttpClient);

  getAccount(accountId: string) {
    return this.client.get<Account>(`api/accounts/${accountId}`);
  }

  getMyAccounts() {
    return this.client.get<Account>('api/my/accounts');
  }

  listAccounts(
    pageSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<AccountFilters>,
  ) {
    return httpResource<ListResult<Account>>(() => {
      const params: ListAccountsParams = {
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
        url: 'api/accounts',
        params: queryParams,
      };
    });
  }
}
