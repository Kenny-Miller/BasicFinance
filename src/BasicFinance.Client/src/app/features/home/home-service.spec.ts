import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AccountClient } from '../../core/data-access/account-client';
import { SpendingClient } from '../../core/data-access/spending-client';
import { TransactionClient } from '../../core/data-access/transaction-client';
import { PageService } from '../../core/page/page.service';
import { HomeService } from './home-service';

describe('HomeService', () => {
  let service: HomeService;

  let balanceSummaryValue = signal<unknown>(null);
  let balanceSummaryHasValue = signal(false);
  let balanceSummaryError = signal<undefined | Error>(undefined);

  let transactionsValue = signal<unknown>(null);
  let transactionsHasValue = signal(false);
  let transactionsError = signal<undefined | Error>(undefined);

  let spendingValue = signal<unknown>(null);
  let spendingHasValue = signal(false);
  let spendingError = signal<undefined | Error>(undefined);

  let institutionsLoading = signal(false);
  let institutionsError = signal<null | Error>(null);
  let refetchCounts = { balance: 0, transactions: 0, spending: 0, institutions: 0 };

  beforeEach(() => {
    balanceSummaryValue = signal(null);
    balanceSummaryHasValue = signal(false);
    balanceSummaryError = signal(undefined);
    transactionsValue = signal(null);
    transactionsHasValue = signal(false);
    transactionsError = signal(undefined);
    spendingValue = signal(null);
    spendingHasValue = signal(false);
    spendingError = signal(undefined);
    institutionsLoading = signal(false);
    institutionsError = signal(null);
    refetchCounts = { balance: 0, transactions: 0, spending: 0, institutions: 0 };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AccountClient,
          useValue: {
            balanceSummaryResource: () => ({
              hasValue: () => balanceSummaryHasValue(),
              error: () => balanceSummaryError(),
              value: () => balanceSummaryValue(),
              reload: () => {
                refetchCounts.balance++;
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
          },
        },
        {
          provide: SpendingClient,
          useValue: {
            spendingOverTimeSummaryResource: () => ({
              hasValue: () => spendingHasValue(),
              error: () => spendingError(),
              value: () => spendingValue(),
              reload: () => {
                refetchCounts.spending++;
                return true;
              },
            }),
          },
        },
        {
          provide: PageService,
          useValue: {
            loading: () => institutionsLoading(),
            error: () => institutionsError(),
            refetchAll: () => {
              refetchCounts.institutions++;
            },
          },
        },
      ],
    });
    service = TestBed.inject(HomeService);
  });

  it('should report loading until every resource has a value', () => {
    expect(service.loading()).toBe(true);

    balanceSummaryHasValue.set(true);
    transactionsHasValue.set(true);
    spendingHasValue.set(true);

    expect(service.loading()).toBe(false);
  });

  it('should report an error when any resource errors', () => {
    expect(service.error()).toBeFalsy();

    transactionsError.set(new Error('boom'));

    expect(service.error()).toBeTruthy();
  });

  it('should keep reporting loading while institutions have not loaded', () => {
    balanceSummaryHasValue.set(true);
    transactionsHasValue.set(true);
    spendingHasValue.set(true);
    institutionsLoading.set(true);

    expect(service.loading()).toBe(true);

    institutionsLoading.set(false);

    expect(service.loading()).toBe(false);
  });

  it('should stop loading when institutions error and the page data is ready', () => {
    balanceSummaryHasValue.set(true);
    transactionsHasValue.set(true);
    spendingHasValue.set(true);
    institutionsError.set(new Error('no institutions'));

    expect(service.loading()).toBe(false);
  });

  it('should surface institutions fetch errors', () => {
    expect(service.error()).toBeFalsy();

    institutionsError.set(new Error('no institutions'));

    expect(service.error()).toBeTruthy();
  });

  it('should map net worth and account type balances from the balance summary', () => {
    const breakdown = {
      balance: 100500,
      accountTypeBreakdowns: {
        CHK: { balance: 40000, percentageOfTotalBalance: 40, accounts: [] },
        SAV: { balance: 35000, percentageOfTotalBalance: 35, accounts: [] },
        INV: { balance: 25500, percentageOfTotalBalance: 25, accounts: [] },
      },
    };
    balanceSummaryValue.set({
      currentPeriodBreakdown: breakdown,
      previousPeriodBreakdown: {
        balance: 90000,
        accountTypeBreakdowns: {
          CHK: { balance: 38000, percentageOfTotalBalance: 42, accounts: [] },
          SAV: { balance: 32000, percentageOfTotalBalance: 35, accounts: [] },
          INV: { balance: 20000, percentageOfTotalBalance: 23, accounts: [] },
        },
      },
    });
    balanceSummaryHasValue.set(true);

    expect(service.data().currentPeriodTotalBalance).toBe(100500);
    expect(service.data().currentPeriodCheckingBalance).toBe(40000);
    expect(service.data().currentPeriodSavingsBalance).toBe(35000);
    expect(service.data().currentPeriodInvestmentsBalance).toBe(25500);
    expect(service.data().previousPeriodBalance).toBe(90000);
    expect(service.data().previousPeriodCheckingBalance).toBe(38000);
    expect(service.data().previousPeriodSavingsBalance).toBe(32000);
    expect(service.data().previousPeriodInvestmentsBalance).toBe(20000);
    expect(service.data().currentPeriodBreakdown).toEqual(breakdown);
  });

  it('should fall back to zero balances and an empty breakdown when the balance summary is missing', () => {
    expect(service.data().currentPeriodTotalBalance).toBe(0);
    expect(service.data().currentPeriodCheckingBalance).toBe(0);
    expect(service.data().currentPeriodSavingsBalance).toBe(0);
    expect(service.data().currentPeriodInvestmentsBalance).toBe(0);
    expect(service.data().previousPeriodBalance).toBe(0);
    expect(service.data().previousPeriodCheckingBalance).toBe(0);
    expect(service.data().previousPeriodSavingsBalance).toBe(0);
    expect(service.data().previousPeriodInvestmentsBalance).toBe(0);
    expect(service.data().currentPeriodBreakdown).toEqual({
      balance: 0,
      accountTypeBreakdowns: {},
    });
  });

  it('should map recent transactions from the transactions list result', () => {
    const transactions = [
      {
        id: '1',
        transactionTypeName: 'Debit',
        transactionCategoryName: 'Food',
        accountName: 'Checking',
        date: '2026-08-01',
        amount: -1234,
        description: 'Groceries',
      },
    ];
    transactionsValue.set({
      items: transactions,
      page: 1,
      pageSize: 5,
      pageCount: 1,
      totalCount: 1,
    });
    transactionsHasValue.set(true);

    expect(service.data().recentTransactions).toEqual(transactions);
  });

  it('should expose the spending over time data', () => {
    const spending = {
      currentMonthActivity: [{ x: 1, y: 100 }],
      previousMonthActivity: [{ x: 1, y: 90 }],
      totalMonthlySpend: 100,
      monthlySpendDifference: 10,
    };
    spendingValue.set(spending);
    spendingHasValue.set(true);

    expect(service.data().spendingOverTime).toEqual(spending);
  });

  it('should expose default spending and transactions when those resources are missing', () => {
    expect(service.data().spendingOverTime).toEqual({
      currentMonthActivity: [],
      previousMonthActivity: [],
      totalMonthlySpend: 0,
      monthlySpendDifference: 0,
    });
    expect(service.data().recentTransactions).toEqual([]);
  });

  it('should reload every resource', () => {
    service.refetchAll();

    expect(refetchCounts).toEqual({
      balance: 1,
      transactions: 1,
      spending: 1,
      institutions: 1,
    });
  });
});
