import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ListResult } from '../../../shared/api/list-result';
import { Transaction } from '../../../shared/api/transactions/transactions';
import { AccountTypeGroup } from '../../../shared/api/accounts/accountByType';

@Injectable({
  providedIn: 'root',
})
export class HomeClient {
  client = inject(HttpClient);

  accountsByTypeResource = httpResource<ListResult<AccountTypeGroup>>(
    () => 'api/accounts/byType',
  );

  transactionsResource = httpResource<ListResult<Transaction>>(() => ({
    url: 'api/transactions',
    params: {
      page: 1,
      pageSize: 5,
    },
  }));

  tempResource = httpResource<any>(() => 'api/reports/spendingActivity');
}
