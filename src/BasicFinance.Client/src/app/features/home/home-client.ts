import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ListResult } from '../../shared/api/list-result';
import { Transaction } from '../../shared/api/transactions/transactions';
import { Account } from '../../shared/api/accounts/account';

@Injectable({
  providedIn: 'root',
})
export class HomeClient {
  client = inject(HttpClient);

  accountsResource = httpResource<ListResult<Account>>(() => ({
    url: 'api/accounts',
    params: {
      page: 1,
      pageSize: 10,
    },
  }));

  transactionsResource = httpResource<ListResult<Transaction>>(() => ({
    url: 'api/transactions',
    params: {
      page: 1,
      pageSize: 10,
    },
  }));
}
