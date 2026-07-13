import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ListResult } from '../../../shared/api/list-result';
import { Transaction } from '../../../shared/api/transactions/transactions';
import { AccountTypeGroup } from '../../../shared/api/accounts/accountByType';
import { SpendingOverTimeSummary } from '../../../shared/api/spending/spending-over-time-summary';
import { NetWorthSummary } from '../../../shared/api/accounts/networth-summary';

@Injectable({
  providedIn: 'root',
})
export class HomeClient {
  accountsByTypeResource = httpResource<ListResult<AccountTypeGroup>>(() => 'api/accounts/byType');

  transactionsResource = httpResource<Transaction[]>(() => 'api/transactions/recent');

  spendingOverTimeResource = httpResource<SpendingOverTimeSummary>(
    () => 'api/Spending/SpendingOverTimeSummary',
  );

  netWorthSummaryResource = httpResource<NetWorthSummary>(() => 'api/accounts/netWorthSummary');
}
