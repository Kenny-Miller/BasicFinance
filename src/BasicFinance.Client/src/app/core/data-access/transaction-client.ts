import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { TimePeriod } from '../../shared/data/time-period';
import { IPagedQuery, ISortedQuery, ListResult } from './api-interfaces';

export interface Transaction {
  id: string;
  transactionTypeName: string;
  transactionCategoryName: string;
  accountName: string;
  date: string;
  amount: number;
  description: string;
}

export type TransactionTypeCode = 'CR' | 'DR';

export interface TransactionFilters {
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: TransactionTypeCode;
  transactionCategoryCode?: string;
  accountId?: string;
  search?: string;
}

interface ListTransactionsParams extends IPagedQuery, ISortedQuery {
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  accountId?: string;
  search?: string;
}

export interface TransactionPeriodSummary {
  totalCount: number;
  totalSpend: number;
  totalIncome: number;
  netFlow: number;
}

export interface TransactionSummaryResponse {
  currentStart: string;
  currentEnd: string;
  previousStart: string;
  previousEnd: string;
  currentPeriod: TransactionPeriodSummary;
  previousPeriod: TransactionPeriodSummary;
}

export interface DailyTransactionPoint {
  date: string;
  totalSpend: number;
  transactionCount: number;
}

export interface DailySummaryResponse {
  currentStart: string;
  currentEnd: string;
  previousStart: string;
  previousEnd: string;
  currentPeriod: DailyTransactionPoint[];
  previousPeriod: DailyTransactionPoint[];
}

@Injectable({
  providedIn: 'root',
})
export class TransactionClient {
  client = inject(HttpClient);

  getTransaction(transactionId: string) {
    return this.client.get<Transaction>(`api/transactions/${transactionId}`);
  }

  listTransactions(
    pageSignal: Signal<number>,
    pageSizeSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
    filtersSignal: Signal<TransactionFilters>,
  ) {
    return httpResource<ListResult<Transaction>>(() => {
      const params: ListTransactionsParams = {
        page: pageSignal(),
        pageSize: pageSizeSignal(),
        sortField: sortFieldSignal(),
        sortDirection: sortDirectionSignal(),
        ...filtersSignal(),
      };

      const queryParams = Object.fromEntries(
        Object.entries(params).filter(([_, value]) => value !== undefined && value !== ''),
      );

      return {
        url: 'api/transactions',
        params: queryParams,
      };
    });
  }

  transactionSummaryResource(
    recordedDateSignal: Signal<Date | null>,
    timePeriodSignal: Signal<TimePeriod>,
  ) {
    return httpResource<TransactionSummaryResponse>(() => {
      const params: Record<string, string> = { TimePeriod: timePeriodSignal() };
      const recordedDate = recordedDateSignal();
      if (recordedDate) {
        params['RecordedDate'] = this._formatDateOnly(recordedDate);
      }
      return {
        url: 'api/transactions/summary',
        params,
      };
    });
  }

  dailyTransactionSummaryResource(
    recordedDateSignal: Signal<Date | null>,
    timePeriodSignal: Signal<TimePeriod>,
  ) {
    return httpResource<DailySummaryResponse>(() => {
      const params: Record<string, string> = { TimePeriod: timePeriodSignal() };
      const recordedDate = recordedDateSignal();
      if (recordedDate) {
        params['RecordedDate'] = this._formatDateOnly(recordedDate);
      }
      return {
        url: 'api/transactions/dailySummary',
        params,
      };
    });
  }

  private _formatDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
