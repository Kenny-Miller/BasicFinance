import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { InstitutionClient } from '../../core/data-access/institution-client';
import { TimePeriod } from '../../shared/data/time-period';
import { PageService } from '../../core/page/page.service';
import { PeriodSelector } from '../../shared/ui/period-selector/period-selector';
import { Account } from './account';
import { AccountPageData, AccountPageService } from './account-page-service';

function createData(overrides: Partial<AccountPageData> = {}): AccountPageData {
  return {
    institutionName: 'Wells Fargo',
    totalBalance: 14500,
    previousTotalBalance: 12000,
    typeCards: [
      { code: 'CHK', label: 'Checking', balance: 5000, previousBalance: 4000 },
      { code: 'SAV', label: 'Savings', balance: 12000, previousBalance: 11000 },
      { code: 'CC', label: 'Credit Cards', balance: -2500, previousBalance: -3000 },
    ],
    accounts: [
      {
        id: 'a1',
        accountTypeCode: 'CHK',
        institution: 'Wells Fargo',
        accountName: 'Checking ****1234',
        balance: 5000,
        percentageOfTotalBalance: 34.48,
        percentageOfAccountTypeBalance: 100,
      },
      {
        id: 'a2',
        accountTypeCode: 'SAV',
        institution: 'Wells Fargo',
        accountName: 'Savings ****5678',
        balance: 12000,
        percentageOfTotalBalance: 82.76,
        percentageOfAccountTypeBalance: 100,
      },
      {
        id: 'a3',
        accountTypeCode: 'CC',
        institution: 'Wells Fargo',
        accountName: 'Platinum ****9012',
        balance: -2500,
        percentageOfTotalBalance: -17.24,
        percentageOfAccountTypeBalance: 100,
      },
    ],
    ...overrides,
  };
}

describe('Account', () => {
  let fixture: ComponentFixture<Account>;
  let paramsSubject = new Subject<Record<string, string>>();
  let loading: WritableSignal<boolean>;
  let error: WritableSignal<unknown>;
  let institutionName: WritableSignal<string>;
  let data: WritableSignal<AccountPageData>;
  let institutionIdCalls: number[] = [];
  let selectPeriodCalls: TimePeriod[] = [];
  let refetchAllCalls = 0;

  beforeEach(async () => {
    paramsSubject = new Subject<Record<string, string>>();
    loading = signal(false);
    error = signal<unknown>(null);
    institutionName = signal('Wells Fargo');
    data = signal(createData());
    institutionIdCalls = [];
    selectPeriodCalls = [];
    refetchAllCalls = 0;
    const selectedPeriod = signal<TimePeriod>('Monthly');

    await TestBed.configureTestingModule({
      imports: [Account],
      providers: [
        {
          provide: AccountPageService,
          useValue: {
            loading,
            error,
            institutionName,
            data,
            selectedPeriod,
            selectPeriod: (period: TimePeriod) => {
              selectPeriodCalls.push(period);
              selectedPeriod.set(period);
            },
            refetchAll: () => {
              refetchAllCalls++;
            },
            setInstitutionId: (id: number) => {
              institutionIdCalls.push(id);
            },
          } as unknown as AccountPageService,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            params: paramsSubject,
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

    fixture = TestBed.createComponent(Account);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should wire the institution id route param to the page service', () => {
    paramsSubject.next({ institutionId: '1' });

    expect(institutionIdCalls).toEqual([1]);
  });

  it('should default the institution id to 0 for a non-numeric or missing route param', () => {
    paramsSubject.next({ institutionId: 'not-a-number' });
    paramsSubject.next({});

    expect(institutionIdCalls).toEqual([0, 0]);
  });

  it('should render the skeleton while loading', () => {
    loading.set(true);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('app-account-skeleton'))).toBeTruthy();
    expect(fixture.debugElement.query(By.css('app-summary-card'))).toBeNull();
  });

  it('should show a retry affordance when the request errors', () => {
    error.set(new Error('boom'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Failed to load account data');

    const retry = fixture.debugElement.query(By.css('button'));
    retry.triggerEventHandler('click', null);

    expect(refetchAllCalls).toBe(1);
  });

  it('should render the institution header, period selector, and balance cards', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Wells Fargo');
    expect(fixture.debugElement.query(By.css('app-period-selector'))).toBeTruthy();

    // Total card + one card per account type
    expect(fixture.debugElement.queryAll(By.css('app-summary-card')).length).toBe(4);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Total');
    expect(text).toContain('Checking');
    expect(text).toContain('Savings');
    expect(text).toContain('Credit Cards');
  });

  it('should render an account item for each account', () => {
    fixture.detectChanges();

    expect(fixture.debugElement.queryAll(By.css('app-account-item')).length).toBe(3);
    expect(fixture.nativeElement.textContent).toContain('Checking ****1234');
    expect(fixture.nativeElement.textContent).toContain('Platinum ****9012');
  });

  it('should show an empty state when the institution has no accounts', () => {
    data.set(createData({ accounts: [] }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No active accounts');
    expect(fixture.debugElement.query(By.css('app-account-item'))).toBeNull();
  });

  it('should set the page title from the institution name', () => {
    fixture.detectChanges();

    const pageService = TestBed.inject(PageService);
    expect(pageService.pageTitle()).toBe('Wells Fargo');
    expect(pageService.pageSubtitle()).toBe('View your account summary and balances.');
  });

  it('should fall back to a default page title when the institution name is empty', () => {
    institutionName.set('');
    fixture.detectChanges();

    const pageService = TestBed.inject(PageService);
    expect(pageService.pageTitle()).toBe('Accounts');
  });

  it('should forward period changes to the service', () => {
    fixture.detectChanges();

    const selector = fixture.debugElement.query(By.directive(PeriodSelector));
    selector.triggerEventHandler('periodChange', 'Quarterly');

    expect(selectPeriodCalls).toEqual(['Quarterly']);
  });
});
