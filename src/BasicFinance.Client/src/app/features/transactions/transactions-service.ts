import { computed, inject, Injectable, signal } from '@angular/core';
import { Account, AccountClient } from '../../core/data-access/account-client';
import { ListResult } from '../../core/data-access/api-interfaces';
import {
  DailySummaryResponse,
  Transaction,
  TransactionClient,
  TransactionFilters,
  TransactionPeriodSummary,
  TransactionSummaryResponse,
} from '../../core/data-access/transaction-client';
import { DEFAULT_TIME_PERIOD, TimePeriod } from '../../shared/data/time-period';

export interface TransactionsData {
  accounts: ListResult<Account>;
  transactions: ListResult<Transaction>;
  dailySummary: DailySummaryResponse;
  transactionSummary: TransactionSummaryResponse;
}

const EMPTY_ACCOUNTS: ListResult<Account> = {
  page: 1,
  pageCount: 0,
  totalCount: 0,
  pageSize: 100,
  items: [],
};

const EMPTY_TRANSACTIONS: ListResult<Transaction> = {
  page: 1,
  pageCount: 0,
  totalCount: 0,
  pageSize: 10,
  items: [],
};

const EMPTY_PERIOD_SUMMARY: TransactionPeriodSummary = {
  totalCount: 0,
  totalSpend: 0,
  totalIncome: 0,
  netFlow: 0,
};

const EMPTY_DAILY_SUMMARY: DailySummaryResponse = {
  currentStart: '',
  currentEnd: '',
  previousStart: '',
  previousEnd: '',
  currentPeriod: [],
  previousPeriod: [],
};

const EMPTY_TRANSACTION_SUMMARY: TransactionSummaryResponse = {
  currentStart: '',
  currentEnd: '',
  previousStart: '',
  previousEnd: '',
  currentPeriod: EMPTY_PERIOD_SUMMARY,
  previousPeriod: EMPTY_PERIOD_SUMMARY,
};

@Injectable({
  providedIn: 'root',
})
export class TransactionsService {
  private readonly transactionsClient = inject(TransactionClient);
  private readonly accountClient = inject(AccountClient);

  readonly period = signal<TimePeriod>(DEFAULT_TIME_PERIOD);
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly sortField = signal<string>('date');
  readonly sortDirection = signal<string>('desc');
  readonly filters = signal<TransactionFilters>({});

  private readonly listAccounts = this.accountClient.listAccounts(
    signal(1),
    signal(100),
    signal('name'),
    signal('asc'),
    signal({}),
  );

  private readonly listTransactions = this.transactionsClient.listTransactions(
    this.page,
    this.pageSize,
    this.sortField,
    this.sortDirection,
    this.filters,
  );
  private readonly transactionSummary = this.transactionsClient.transactionSummaryResource(
    signal<Date | null>(null),
    this.period,
  );

  private readonly dailySummary = this.transactionsClient.dailyTransactionSummaryResource(
    signal<Date | null>(null),
    this.period,
  );

  readonly loading = computed(
    () =>
      !this.listAccounts.hasValue() &&
      !this.listTransactions.hasValue() &&
      !this.dailySummary.hasValue() &&
      !this.transactionSummary.hasValue(),
  );

  readonly transactionsLoading = computed(() => !this.listTransactions.hasValue());

  private readonly hasSeenTransactions = signal(false);

  hasTransactionsData(): boolean {
    if (this.listTransactions.hasValue() && !this.hasSeenTransactions()) {
      this.hasSeenTransactions.set(true);
    }
    return this.hasSeenTransactions();
  }

  readonly error = computed(
    () =>
      this.listAccounts.error() ||
      this.listTransactions.error() ||
      this.dailySummary.error() ||
      this.transactionSummary.error(),
  );

  readonly data = computed<TransactionsData>(() => {
    const accounts = this.listAccounts.value();
    const transactions = this.listTransactions.value();
    const dailySummary = this.dailySummary.value();
    const transactionSummary = this.transactionSummary.value();

    return {
      accounts: accounts ?? EMPTY_ACCOUNTS,
      transactions: transactions ?? EMPTY_TRANSACTIONS,
      dailySummary: dailySummary ?? EMPTY_DAILY_SUMMARY,
      transactionSummary: transactionSummary ?? EMPTY_TRANSACTION_SUMMARY,
    };
  });

  refetchAll(): void {
    this.listAccounts.reload();
    this.listTransactions.reload();
    this.transactionSummary.reload();
    this.dailySummary.reload();
  }
}
