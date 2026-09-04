import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Account, AccountClient } from '../../core/data-access/account-client';
import { ListResult } from '../../core/data-access/api-interfaces';
import {
  DailySummaryResponse,
  Transaction,
  TransactionClient,
  TransactionSummaryResponse,
} from '../../core/data-access/transaction-client';
import { DEFAULT_TIME_PERIOD } from '../../shared/data/time-period';
import { TransactionsService } from './transactions-service';

describe('TransactionsService', () => {
  let service: TransactionsService;

  let accountsValue = signal<ListResult<Account> | null>(null);
  let accountsHasValue = signal(false);
  let accountsError = signal<undefined | Error>(undefined);

  let transactionsValue = signal<ListResult<Transaction> | null>(null);
  let transactionsHasValue = signal(false);
  let transactionsError = signal<undefined | Error>(undefined);

  let summaryValue = signal<TransactionSummaryResponse | null>(null);
  let summaryHasValue = signal(false);
  let summaryError = signal<undefined | Error>(undefined);

  let dailyValue = signal<DailySummaryResponse | null>(null);
  let dailyHasValue = signal(false);
  let dailyError = signal<undefined | Error>(undefined);

  let refetchCounts = { accounts: 0, transactions: 0, summary: 0, daily: 0 };

  beforeEach(() => {
    accountsValue = signal(null);
    accountsHasValue = signal(false);
    accountsError = signal(undefined);
    transactionsValue = signal(null);
    transactionsHasValue = signal(false);
    transactionsError = signal(undefined);
    summaryValue = signal(null);
    summaryHasValue = signal(false);
    summaryError = signal(undefined);
    dailyValue = signal(null);
    dailyHasValue = signal(false);
    dailyError = signal(undefined);
    refetchCounts = { accounts: 0, transactions: 0, summary: 0, daily: 0 };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AccountClient,
          useValue: {
            listAccounts: () => ({
              hasValue: () => accountsHasValue(),
              error: () => accountsError(),
              value: () => accountsValue(),
              reload: () => {
                refetchCounts.accounts++;
                return true;
              },
            }),
          },
        },
        {
          provide: TransactionClient,
          useValue: {
            listTransactions: () => ({
              hasValue: () => transactionsHasValue(),
              error: () => transactionsError(),
              value: () => transactionsValue(),
              reload: () => {
                refetchCounts.transactions++;
                return true;
              },
            }),
            transactionSummaryResource: () => ({
              hasValue: () => summaryHasValue(),
              error: () => summaryError(),
              value: () => summaryValue(),
              reload: () => {
                refetchCounts.summary++;
                return true;
              },
            }),
            dailyTransactionSummaryResource: () => ({
              hasValue: () => dailyHasValue(),
              error: () => dailyError(),
              value: () => dailyValue(),
              reload: () => {
                refetchCounts.daily++;
                return true;
              },
            }),
          },
        },
      ],
    });
    service = TestBed.inject(TransactionsService);
  });

  it('should default to the monthly period on page one with descending date sort', () => {
    expect(service.period()).toBe(DEFAULT_TIME_PERIOD);
    expect(service.page()).toBe(1);
    expect(service.pageSize()).toBe(10);
    expect(service.sortField()).toBe('date');
    expect(service.sortDirection()).toBe('desc');
    expect(service.filters()).toEqual({});
  });

  it('should report loading until any resource has a value', () => {
    expect(service.loading()).toBe(true);

    transactionsHasValue.set(true);

    expect(service.loading()).toBe(false);
  });

  it('should keep the transaction list loading while its resource refetches', () => {
    transactionsHasValue.set(true);

    expect(service.transactionsLoading()).toBe(false);

    transactionsHasValue.set(false);

    expect(service.transactionsLoading()).toBe(true);
  });

  it('should keep the latest transactions visible while the list refetches', () => {
    @Component({ template: '' })
    class TestHost {}

    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();

    expect(service.hasTransactionsData()).toBe(false);
    expect(service.data().transactions).toEqual({
      page: 1,
      pageCount: 0,
      totalCount: 0,
      pageSize: 10,
      items: [],
    });

    const transactions: ListResult<Transaction> = {
      page: 1,
      pageCount: 1,
      totalCount: 1,
      pageSize: 10,
      items: [
        {
          id: '1',
          transactionTypeName: 'Credit',
          transactionCategoryName: 'Income',
          accountName: 'Checking',
          date: '2026-08-01',
          amount: 4200,
          description: 'Salary',
        },
      ],
    };
    transactionsValue.set(transactions);
    transactionsHasValue.set(true);
    fixture.detectChanges();

    expect(service.hasTransactionsData()).toBe(true);
    expect(service.data().transactions).toEqual(transactions);

    transactionsHasValue.set(false);
    fixture.detectChanges();

    expect(service.transactionsLoading()).toBe(true);
    expect(service.hasTransactionsData()).toBe(true);
    expect(service.data().transactions).toEqual(transactions);
  });

  it('should report an error when any resource errors', () => {
    expect(service.error()).toBeFalsy();

    summaryError.set(new Error('boom'));

    expect(service.error()).toBeTruthy();
  });

  it('should expose the transaction data from the resources', () => {
    const accounts: ListResult<Account> = {
      page: 1,
      pageSize: 100,
      pageCount: 1,
      totalCount: 1,
      items: [
        {
          id: '1',
          name: 'Checking',
          accountTypeCode: 'CHK',
          accountTypeName: 'Checkings',
          institutionCode: 'CHAS',
          institutionName: 'Chase',
          currency: 'USD',
          isLiability: false,
          latestBalance: 40000,
          latestBalanceRecordedDate: '2026-08-31',
        },
      ],
    };
    const transactions: ListResult<Transaction> = {
      page: 1,
      pageSize: 10,
      pageCount: 1,
      totalCount: 1,
      items: [
        {
          id: '1',
          transactionTypeName: 'Debit',
          transactionCategoryName: 'Food',
          accountName: 'Checking',
          date: '2026-08-01',
          amount: -1234,
          description: 'Groceries',
        },
      ],
    };
    const summary: TransactionSummaryResponse = {
      currentStart: '2026-08-01',
      currentEnd: '2026-08-31',
      previousStart: '2026-07-01',
      previousEnd: '2026-07-31',
      currentPeriod: { totalCount: 12, totalSpend: 500, totalIncome: 3000, netFlow: 2500 },
      previousPeriod: { totalCount: 10, totalSpend: 400, totalIncome: 2800, netFlow: 2400 },
    };
    const daily: DailySummaryResponse = {
      currentStart: '2026-08-01',
      currentEnd: '2026-08-31',
      previousStart: '2026-07-01',
      previousEnd: '2026-07-31',
      currentPeriod: [{ date: '2026-08-01', totalSpend: 100, transactionCount: 2 }],
      previousPeriod: [],
    };
    accountsValue.set(accounts);
    accountsHasValue.set(true);
    transactionsValue.set(transactions);
    transactionsHasValue.set(true);
    summaryValue.set(summary);
    summaryHasValue.set(true);
    dailyValue.set(daily);
    dailyHasValue.set(true);

    expect(service.data()).toEqual({
      accounts,
      transactions,
      dailySummary: daily,
      transactionSummary: summary,
    });
  });

  it('should fall back to empty transaction data when the resources have no value', () => {
    expect(service.data()).toEqual({
      accounts: { page: 1, pageCount: 0, totalCount: 0, pageSize: 100, items: [] },
      transactions: { page: 1, pageCount: 0, totalCount: 0, pageSize: 10, items: [] },
      dailySummary: {
        currentStart: '',
        currentEnd: '',
        previousStart: '',
        previousEnd: '',
        currentPeriod: [],
        previousPeriod: [],
      },
      transactionSummary: {
        currentStart: '',
        currentEnd: '',
        previousStart: '',
        previousEnd: '',
        currentPeriod: { totalCount: 0, totalSpend: 0, totalIncome: 0, netFlow: 0 },
        previousPeriod: { totalCount: 0, totalSpend: 0, totalIncome: 0, netFlow: 0 },
      },
    });
  });

  it('should reload every resource', () => {
    service.refetchAll();

    expect(refetchCounts).toEqual({ accounts: 1, transactions: 1, summary: 1, daily: 1 });
  });
});
