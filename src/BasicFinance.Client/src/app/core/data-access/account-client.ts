import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, inject, resource } from '@angular/core';

import { TimePeriod } from '../../shared/data/time-period';
import {
  getApiAccounts,
  getApiAccountsBalanceSummary,
  getApiAccountsInstitutionInstitutionIdSummary,
} from '../api/generated/sdk.gen';
import type {
  AccountBalanceDto,
  AccountDto,
  AccountTypeBreakdown,
  GetApiAccountsData,
  InstitutionSummaryResponse,
  TotalBalanceBreakdown,
} from '../api/generated/types.gen';
import { normalizeListResult } from '../api/list-result';
import { TIME_PERIOD_PARAM } from '../api/period-params';

export type Account = AccountDto;

export type {
  AccountBalanceDto,
  AccountTypeBreakdown,
  InstitutionSummaryResponse,
  TotalBalanceBreakdown,
};

export interface AccountFilters {
  accountTypeCode?: string;
  institution?: string;
}

export interface AccountPagingFilters extends AccountFilters {
  page: number;
  pageSize: number;
  sortField: string;
  sortDirection: string;
}

function toAccountsQuery(params: AccountPagingFilters): NonNullable<GetApiAccountsData['query']> {
  return {
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortDirection: params.sortDirection,
    AccountTypeCode: params.accountTypeCode,
    Institution: params.institution,
  };
}

@Injectable({
  providedIn: 'root',
})
export class AccountClient {
  private readonly httpClient = inject(HttpClient);

  listAccounts(
    pageSignal: Signal<number>,
    pageSizeSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<AccountFilters>
  ) {
    return resource({
      params: () => ({
        page: pageSignal(),
        pageSize: pageSizeSignal(),
        sortField: sortFieldSignal(),
        sortDirection: sortDirectionSignal(),
        ...filtersSignal(),
      }),
      loader: async ({ params }) => {
        const response = await getApiAccounts({
          httpClient: this.httpClient,
          query: toAccountsQuery(params),
          throwOnError: true,
        });
        return normalizeListResult(response.data);
      },
    });
  }

  balanceSummaryResource(timePeriodSignal: Signal<TimePeriod>) {
    return resource({
      params: () => ({ TimePeriod: TIME_PERIOD_PARAM[timePeriodSignal()] }),
      loader: async ({ params }) => {
        const response = await getApiAccountsBalanceSummary({
          httpClient: this.httpClient,
          query: params,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }

  institutionSummaryResource(
    institutionIdSignal: Signal<number>,
    timePeriodSignal: Signal<TimePeriod>
  ) {
    return resource({
      params: () => ({
        institutionId: institutionIdSignal(),
        TimePeriod: TIME_PERIOD_PARAM[timePeriodSignal()],
      }),
      loader: async ({ params }) => {
        const response = await getApiAccountsInstitutionInstitutionIdSummary({
          httpClient: this.httpClient,
          path: { institutionId: params.institutionId },
          query: { TimePeriod: params.TimePeriod },
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
