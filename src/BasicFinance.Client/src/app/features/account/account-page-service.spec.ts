import { signal, Signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  Account,
  AccountClient,
  InstitutionSummaryResponse,
} from '../../core/data-access/account-client';
import { PageService } from '../../core/page/page.service';
import { TimePeriod } from '../../shared/data/time-period';
import { AccountPageService } from './account-page-service';

function makeAccount(id: string, code: string, name: string, balance: number | null): Account {
  return {
    id,
    name,
    accountTypeCode: code,
    accountTypeName: code,
    institutionCode: 'WF',
    institutionName: 'Wells Fargo',
    currency: 'USD',
    isLiability: code === 'CC',
    latestBalance: balance,
    latestBalanceRecordedDate: '2026-09-01T00:00:00Z',
  };
}

function makeSummary(
  overrides: Partial<InstitutionSummaryResponse> = {},
): InstitutionSummaryResponse {
  return {
    institutionId: 1,
    institutionName: 'Wells Fargo',
    accounts: [
      makeAccount('a1', 'CHK', 'Checking ****1234', 5000),
      makeAccount('a2', 'SAV', 'Savings ****5678', 12000),
      makeAccount('a3', 'CC', 'Platinum ****9012', -2500),
    ],
    accountTypeTotals: { CHK: 5000, SAV: 12000, INV: 0, CC: -2500 },
    accountTypePreviousTotals: { CHK: 4000, SAV: 11000, INV: 0, CC: -3000 },
    currentPeriodStart: '2026-09-01',
    currentPeriodEnd: '2026-10-01',
    previousPeriodStart: '2026-08-01',
    previousPeriodEnd: '2026-09-01',
    ...overrides,
  };
}

describe('AccountPageService', () => {
  let service: AccountPageService;

  let summaryValue = signal<InstitutionSummaryResponse | null>(null);
  let summaryHasValue = signal(false);
  let summaryError = signal<null | Error>(null);
  let reloadCount = 0;

  let institutionsLoading = signal(false);
  let institutionsError = signal<null | Error>(null);
  let institutionsReloadCount = 0;

  let providedInstitutionId: Signal<number> | undefined;
  let providedTimePeriod: Signal<TimePeriod> | undefined;

  beforeEach(() => {
    summaryValue = signal<InstitutionSummaryResponse | null>(null);
    summaryHasValue = signal(false);
    summaryError = signal<null | Error>(null);
    reloadCount = 0;
    institutionsLoading = signal(false);
    institutionsError = signal(null);
    institutionsReloadCount = 0;
    providedInstitutionId = undefined;
    providedTimePeriod = undefined;

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AccountClient,
          useValue: {
            institutionSummaryResource: (
              institutionId: Signal<number>,
              timePeriod: Signal<TimePeriod>,
            ) => {
              providedInstitutionId = institutionId;
              providedTimePeriod = timePeriod;
              return {
                hasValue: () => summaryHasValue(),
                error: () => summaryError(),
                value: () => summaryValue(),
                reload: () => {
                  reloadCount++;
                  return true;
                },
              };
            },
          },
        },
        {
          provide: PageService,
          useValue: {
            loading: () => institutionsLoading(),
            error: () => institutionsError(),
            refetchAll: () => {
              institutionsReloadCount++;
            },
            accountTypes: () => [
              { code: 'CHK', name: 'Checking' },
              { code: 'SAV', name: 'Savings' },
              { code: 'INV', name: 'Investment' },
              { code: 'CC', name: 'Credit Card' },
            ],
          },
        },
      ],
    });
    service = TestBed.inject(AccountPageService);
  });

  it('should update the institution id supplied to the resource', () => {
    service.setInstitutionId(1);

    expect(service.institutionId()).toBe(1);
    expect(providedInstitutionId?.()).toBe(1);
  });

  it('should report loading until the summary resource has a value', () => {
    expect(service.loading()).toBe(true);

    summaryHasValue.set(true);

    expect(service.loading()).toBe(false);
  });

  it('should expose resource errors', () => {
    expect(service.error()).toBeNull();

    summaryError.set(new Error('boom'));

    expect(service.error()).toBeTruthy();
  });

  it('should keep reporting loading while institutions have not loaded', () => {
    summaryHasValue.set(true);
    institutionsLoading.set(true);

    expect(service.loading()).toBe(true);

    institutionsLoading.set(false);

    expect(service.loading()).toBe(false);
  });

  it('should stop loading when institutions error and the summary is ready', () => {
    summaryHasValue.set(true);
    institutionsError.set(new Error('no institutions'));

    expect(service.loading()).toBe(false);
  });

  it('should surface institutions fetch errors', () => {
    expect(service.error()).toBeNull();

    institutionsError.set(new Error('no institutions'));

    expect(service.error()).toBeTruthy();
  });

  it('should map totals and type cards for account types that have accounts', () => {
    summaryValue.set(makeSummary());
    summaryHasValue.set(true);

    const data = service.data();

    expect(data.institutionName).toBe('Wells Fargo');
    expect(data.totalBalance).toBe(14500);
    expect(data.previousTotalBalance).toBe(12000);
    expect(data.typeCards).toEqual([
      { code: 'CHK', label: 'Checking', balance: 5000, previousBalance: 4000 },
      { code: 'SAV', label: 'Savings', balance: 12000, previousBalance: 11000 },
      { code: 'CC', label: 'Credit Card', balance: -2500, previousBalance: -3000 },
    ]);
  });

  it('should map accounts with percentage breakdowns', () => {
    summaryValue.set(makeSummary());
    summaryHasValue.set(true);

    const accounts = service.data().accounts;

    expect(accounts.length).toBe(3);
    expect(accounts[0]).toEqual({
      id: 'a1',
      accountTypeCode: 'CHK',
      institution: 'Wells Fargo',
      accountName: 'Checking ****1234',
      balance: 5000,
      percentageOfTotalBalance: (5000 / 14500) * 100,
      percentageOfAccountTypeBalance: 100,
    });
    expect(accounts[1].balance).toBe(12000);
    expect(accounts[2].balance).toBe(-2500);
    expect(accounts[2].percentageOfAccountTypeBalance).toBe(100);
  });

  it('should treat a null latest balance as zero without producing NaN percentages', () => {
    summaryValue.set(
      makeSummary({
        accounts: [makeAccount('a1', 'CHK', 'Checking', null)],
        accountTypeTotals: { CHK: 0, SAV: 0, INV: 0, CC: 0 },
        accountTypePreviousTotals: { CHK: 0, SAV: 0, INV: 0, CC: 0 },
      }),
    );
    summaryHasValue.set(true);

    const account = service.data().accounts[0];

    expect(account.balance).toBe(0);
    expect(account.percentageOfTotalBalance).toBe(0);
    expect(account.percentageOfAccountTypeBalance).toBe(0);
  });

  it('should return empty data when the summary has no accounts', () => {
    summaryValue.set(
      makeSummary({
        accounts: [],
        accountTypeTotals: { CHK: 0, SAV: 0, INV: 0, CC: 0 },
        accountTypePreviousTotals: { CHK: 0, SAV: 0, INV: 0, CC: 0 },
      }),
    );
    summaryHasValue.set(true);

    expect(service.data()).toEqual({
      institutionName: 'Wells Fargo',
      totalBalance: 0,
      previousTotalBalance: 0,
      typeCards: [],
      accounts: [],
    });
  });

  it('should return empty data before the resource has a value', () => {
    expect(service.data().totalBalance).toBe(0);
    expect(service.data().typeCards).toEqual([]);
    expect(service.data().accounts).toEqual([]);
    expect(service.institutionName()).toBe('');
  });

  it('should switch the time period used by the resource', () => {
    expect(service.selectedPeriod()).toBe('Monthly');

    service.selectPeriod('Quarterly');

    expect(service.selectedPeriod()).toBe('Quarterly');
    expect(providedTimePeriod?.()).toBe('Quarterly');
  });

  it('should reload the summary resource', () => {
    service.refetchAll();

    expect(reloadCount).toBe(1);
    expect(institutionsReloadCount).toBe(1);
  });
});
