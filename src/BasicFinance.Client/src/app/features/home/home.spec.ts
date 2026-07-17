import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { of } from 'rxjs';
import { ENVIRONMENT_CONFIG } from '../../environment-config';
import { ThemeService } from '../../core/theme/theme.service';
import { HomeClient } from './data/home-client';
import { Home } from './home';

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
          provide: HomeClient,
          useValue: {
            accountsByTypeResource: { value: () => ({ items: [], totalCount: 0, page: 1 }) },
            transactionsResource: { value: () => [] },
            spendingOverTimeResource: { value: () => (null) },
            netWorthSummaryResource: { value: () => (null) },
          },
        },
        {
          provide: ThemeService,
          useValue: {
            appTheme: { getValue: () => 'light' },
            getAppColor: () => '#000000',
          },
        },
        {
          provide: BreakpointObserver,
          useValue: {
            observe: () => of({ matches: false }),
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
