import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { ENVIRONMENT_CONFIG } from '../../environment-config';
import { AccountTypeClient } from '../../core/data-access/account-type-client';
import { InstitutionClient } from '../../core/data-access/institution-client';
import { TransactionCategoryClient } from '../../core/data-access/transaction-category-client';
import { TransactionTypeClient } from '../../core/data-access/transaction-type-client';
import { ThemeService } from '../../core/theme/theme.service';
import { Home } from './home';
import { HomeService } from './home-service';

describe('Home', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideOAuthClient(),
        {
          provide: ENVIRONMENT_CONFIG,
          useValue: {
            basicFinanceApi: 'http://localhost:5001',
            openIdAuthority: 'https://localhost:8080/realms/basic-hub',
            openIdClientId: 'basic-finance-client',
          },
        },
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: AccountTypeClient,
          useValue: {
            accountTypes: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: TransactionTypeClient,
          useValue: {
            transactionTypes: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: TransactionCategoryClient,
          useValue: {
            transactionCategories: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: HomeService,
          useValue: {
            loading: () => false,
            error: () => false,
            data: () => ({
              currentPeriodTotalBalance: 100500,
              currentPeriodCheckingBalance: 40000,
              currentPeriodSavingsBalance: 35000,
              currentPeriodInvestmentsBalance: 25500,
              previousPeriodBalance: 90000,
              previousPeriodCheckingBalance: 38000,
              previousPeriodSavingsBalance: 32000,
              previousPeriodInvestmentsBalance: 20000,
              currentPeriodBreakdown: {
                balance: 100500,
                accountTypeBreakdowns: {},
              },
              spendingOverTime: {
                currentMonthActivity: [],
                previousMonthActivity: [],
                totalMonthlySpend: 0,
                monthlySpendDifference: 0,
              },
              recentTransactions: [],
            }),
            refetchAll: () => ({}),
          },
        },
        {
          provide: ThemeService,
          useValue: {
            appTheme: () => 'light',
          },
        },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Home);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });
});
