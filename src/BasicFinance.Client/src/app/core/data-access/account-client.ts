import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { TimePeriod } from '../../shared/data/time-period';
import { IPagedQuery, ISortedQuery, ListResult } from './api-interfaces';

export interface Account {
  id: string;
  name: string;
  accountTypeCode: string;
  accountTypeName: string;
  institutionCode: string;
  institutionName: string;
  currency: string;
  isLiability: boolean;
  latestBalance: number | null;
  latestBalanceRecordedDate: string | null;
}

export interface AccountFilters {
  accountTypeCode?: string;
  institution?: string;
}

interface ListAccountsParams extends IPagedQuery, ISortedQuery {
  accountTypeCode?: string;
  institution?: string;
}

export interface AccountBalanceDto {
  id: string;
  accountTypeCode: string;
  institution: string;
  accountName: string;
  balance: number;
  percentageOfTotalBalance: number;
  percentageOfAccountTypeBalance: number;
}

export interface AccountTypeBreakdown {
  balance: number;
  percentageOfTotalBalance: number;
  accounts: AccountBalanceDto[];
}

export interface TotalBalanceBreakdown {
  balance: number;
  accountTypeBreakdowns: Record<string, AccountTypeBreakdown>;
}

export interface AccountAnalyticsResponse {
  currentPeriodBreakdown: TotalBalanceBreakdown;
  previousPeriodBreakdown: TotalBalanceBreakdown;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  previousPeriodStart: string;
  previousPeriodEnd: string;
}

export interface InstitutionSummaryResponse {
  institutionId: number;
  institutionName: string;
  accounts: Account[];
  accountTypeTotals: Record<string, number>;
  accountTypePreviousTotals: Record<string, number>;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  previousPeriodStart: string;
  previousPeriodEnd: string;
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
    return this.client.get<Account[]>('api/my/accounts');
  }

  listAccounts(
    pageSignal: Signal<number>,
    pageSizeSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<AccountFilters>,
  ) {
    return httpResource<ListResult<Account>>(() => {
      const params: ListAccountsParams = {
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
        url: 'api/accounts',
        params: queryParams,
      };
    });
  }

  balanceSummaryResource(timePeriodSignal: Signal<TimePeriod>) {
    return httpResource<AccountAnalyticsResponse>(
      () => `api/accounts/balanceSummary?TimePeriod=${timePeriodSignal()}`,
    );
  }

  institutionSummaryResource(
    institutionIdSignal: Signal<number>,
    timePeriodSignal: Signal<TimePeriod>,
  ) {
    return httpResource<InstitutionSummaryResponse>(() => ({
      url: `api/accounts/institution/${institutionIdSignal()}/summary`,
      params: { TimePeriod: timePeriodSignal() },
    }));
  }
}
