import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ListResult } from '../../../shared/api/list-result';
import { Transaction } from '../../../shared/api/transactions/transactions';
import { SpendingOverTimeSummary } from '../../../shared/api/spending/spending-over-time-summary';
import { AccountAnalyticsResponse } from '../../../shared/api/accounts/account-analytics';

@Injectable({
  providedIn: 'root',
})
export class HomeClient {
  balanceSummaryResource = httpResource<AccountAnalyticsResponse>(
    () => 'api/accounts/balanceSummary?TimePeriod=Monthly',
  );

  transactionsResource = httpResource<ListResult<Transaction>>(
    () => 'api/transactions?page=1&pageSize=5&sortField=Date&sortDirection=Descending',
  );

  spendingOverTimeResource = httpResource<SpendingOverTimeSummary>(
    () => 'api/Spending/SpendingOverTimeSummary',
  );
}
