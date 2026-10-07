import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, inject, resource } from '@angular/core';

import { TimePeriod } from '../../shared/data/time-period';
import {
  getApiTransactions,
  getApiTransactionsDailySummary,
  getApiTransactionsSummary,
} from '../api/generated/sdk.gen';
import type { GetApiTransactionsData } from '../api/generated/types.gen';
import { normalizeListResult } from '../api/list-result';
import { TIME_PERIOD_PARAM } from '../api/period-params';

export type {
  DailySummaryResponse,
  DailyTransactionSummary as DailyTransactionPoint,
  TransactionDto as Transaction,
  TransactionPeriodSummary,
  TransactionSummaryResponse,
} from '../api/generated/types.gen';

export interface TransactionFilters {
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  accountId?: string;
  search?: string;
}

interface ListTransactionsParams extends TransactionFilters {
  page: number;
  pageSize: number;
  sortField: string;
  sortDirection: string;
}

type TransactionsQuery = NonNullable<GetApiTransactionsData['query']>;

function toTransactionsQuery(params: ListTransactionsParams): TransactionsQuery {
  return {
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField || undefined,
    SortDirection: params.sortDirection || undefined,
    StartDate: params.startDate || undefined,
    EndDate: params.endDate || undefined,
    MinAmount: params.minAmount,
    MaxAmount: params.maxAmount,
    TransactionTypeCode: params.transactionTypeCode || undefined,
    TransactionCategoryCode: params.transactionCategoryCode || undefined,
    AccountId: params.accountId || undefined,
    Search: params.search || undefined,
  };
}

function formatDateOnly(date: Date | null): string | undefined {
  if (!date) {
    return undefined;
  }
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionClient {
  private readonly httpClient = inject(HttpClient);

  listTransactions(
    pageSignal: Signal<number>,
    pageSizeSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<TransactionFilters>
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
        const response = await getApiTransactions({
          httpClient: this.httpClient,
          query: toTransactionsQuery(params),
          throwOnError: true,
        });
        return normalizeListResult(response.data);
      },
    });
  }

  transactionSummaryResource(
    recordedDateSignal: Signal<Date | null>,
    timePeriodSignal: Signal<TimePeriod>
  ) {
    return resource({
      params: () => ({
        RecordedDate: formatDateOnly(recordedDateSignal()),
        TimePeriod: TIME_PERIOD_PARAM[timePeriodSignal()],
      }),
      loader: async ({ params }) => {
        const response = await getApiTransactionsSummary({
          httpClient: this.httpClient,
          query: params,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }

  dailyTransactionSummaryResource(
    recordedDateSignal: Signal<Date | null>,
    timePeriodSignal: Signal<TimePeriod>
  ) {
    return resource({
      params: () => ({
        RecordedDate: formatDateOnly(recordedDateSignal()),
        TimePeriod: TIME_PERIOD_PARAM[timePeriodSignal()],
      }),
      loader: async ({ params }) => {
        const response = await getApiTransactionsDailySummary({
          httpClient: this.httpClient,
          query: params,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
