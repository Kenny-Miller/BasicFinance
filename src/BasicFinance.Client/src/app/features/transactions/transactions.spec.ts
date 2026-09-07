import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { InstitutionClient } from '../../core/data-access/institution-client';
import { ThemeService } from '../../core/theme/theme.service';
import { Transactions } from './transactions';
import { TransactionsService } from './transactions-service';

describe('Transactions', () => {
  function service(summaryError: boolean) {
    return {
      period: signal('Monthly'),
      page: signal(1),
      pageSize: signal(10),
      filters: signal({}),
      loading: () => false,
      transactionsLoading: () => false,
      hasTransactionsData: () => false,
      error: summaryError ? () => new Error('simulated resource error') : () => undefined,
      data: () => ({
        accounts: { page: 1, pageSize: 100, pageCount: 0, totalCount: 0, items: [] },
        transactions: { page: 1, pageSize: 10, pageCount: 0, totalCount: 0, items: [] },
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
      }),
      refetchAll: () => undefined,
    };
  }

  async function configure(summaryError: boolean) {
    await TestBed.configureTestingModule({
      imports: [Transactions],
      providers: [
        {
          provide: TransactionsService,
          useValue: service(summaryError),
        },
        {
          provide: ThemeService,
          useValue: {
            appTheme: signal('light'),
          },
        },
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: {
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            },
            refetchMyInstitutions: () => undefined,
          },
        },
      ],
    }).compileComponents();
  }

  beforeEach(async () => {
    await configure(false);
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Transactions);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render without throwing when a resource is in an error state', async () => {
    await configure(true);
    const fixture = TestBed.createComponent(Transactions);

    expect(() => fixture.detectChanges()).not.toThrow();
    expect(fixture.componentInstance).toBeTruthy();
  });
});
