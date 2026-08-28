import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { ENVIRONMENT_CONFIG } from '../../environment-config';
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
          provide: HomeService,
          useValue: {
            loading: () => false,
            error: () => false,
            currentNetWorth: () => 100500,
            previousNetWorth: () => 90000,
            currentChecking: () => 40000,
            previousChecking: () => 38000,
            currentSavings: () => 35000,
            previousSavings: () => 32000,
            currentInvestments: () => 25500,
            previousInvestments: () => 20000,
            currentPeriodBreakdown: () => ({
              balance: 100500,
              accountTypeBreakdowns: {},
            }),
            spendingOverTimeData: () => ({
              currentMonthActivity: [],
              previousMonthActivity: [],
              totalMonthlySpend: 0,
              monthlySpendDifference: 0,
            }),
            recentTransactions: () => [],
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
