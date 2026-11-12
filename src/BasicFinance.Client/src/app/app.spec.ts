import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

import { vi } from 'vite-plus/test';

import { App } from './app';
import { GetMyInstitutionsResponse } from './core/api/generated';
import { AccountTypeClient } from './core/data-access/account-type-client';
import { InstitutionClient } from './core/data-access/institution-client';
import { TransactionCategoryClient } from './core/data-access/transaction-category-client';
import { TransactionTypeClient } from './core/data-access/transaction-type-client';

describe('App', () => {
  let institutions: WritableSignal<GetMyInstitutionsResponse[] | null>;

  beforeEach(async () => {
    institutions = signal<GetMyInstitutionsResponse[] | null>(null);

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: () => ({
              hasValue: () => institutions() !== null,
              value: () => institutions(),
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
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should start without account navigation items', () => {
    const app = TestBed.createComponent(App).componentInstance;

    expect(app.accountNavigationItems()).toEqual([]);
  });

  it("should build account navigation items from the user's institutions", () => {
    const app = TestBed.createComponent(App).componentInstance;

    institutions.set([
      {
        institutionId: 1,
        institutionCode: 'WF',
        name: 'Wells Fargo',
        logoUrl: null,
        activeAccounts: 2,
        totalBalance: 1200.5,
      },
      {
        institutionId: 2,
        institutionCode: 'CHAS',
        name: 'Chase',
        logoUrl: 'https://example.com/chase.png',
        activeAccounts: 1,
        totalBalance: 400,
      },
    ]);

    expect(app.accountNavigationItems()).toEqual([
      {
        label: 'Wells Fargo',
        icon: 'lucideLandmark',
        routerLink: 'Accounts/1',
        activeAccounts: 2,
        totalBalance: 1200.5,
      },
      {
        label: 'Chase',
        icon: 'lucideLandmark',
        routerLink: 'Accounts/2',
        activeAccounts: 1,
        totalBalance: 400,
      },
    ]);
  });

  it('should show a skeleton for the account navigation items while institutions are loading', () => {
    const matchMediaSpy = forceDesktopSidebarLayout();

    try {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Institutions');
      const skeletons = fixture.debugElement.queryAll(
        By.css('[data-slot="sidebar-menu-skeleton"]'),
      );
      expect(skeletons.length).toBe(3);
    } finally {
      matchMediaSpy.mockRestore();
    }
  });

  it('should render an account navigation item for each institution', () => {
    const matchMediaSpy = forceDesktopSidebarLayout();

    try {
      const fixture = TestBed.createComponent(App);

      institutions.set([
        {
          institutionId: 7,
          institutionCode: 'DISC',
          name: 'Discover',
          logoUrl: null,
          activeAccounts: 1,
          totalBalance: 750.25,
        },
      ]);
      fixture.detectChanges();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Institutions');
      expect(text).toContain('Discover');

      const accountLink = fixture.debugElement.query(By.css('a[href*="Accounts/7"]'));
      expect(accountLink).toBeTruthy();

      const skeletons = fixture.debugElement.queryAll(
        By.css('[data-slot="sidebar-menu-skeleton"]'),
      );
      expect(skeletons.length).toBe(0);
    } finally {
      matchMediaSpy.mockRestore();
    }
  });
});

function forceDesktopSidebarLayout() {
  // The test viewport (768px) matches the sidebar's mobile breakpoint, which would render
  // the sidebar content inside a closed sheet. Force the desktop layout so the sidebar
  // content is rendered inline in the DOM.
  return vi.spyOn(window, 'matchMedia').mockReturnValue({
    matches: false,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
  } as unknown as MediaQueryList);
}
