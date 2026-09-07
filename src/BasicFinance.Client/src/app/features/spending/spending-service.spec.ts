import { Signal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SpendingByPeriod, SpendingClient } from '../../core/data-access/spending-client';
import { PageService } from '../../core/page/page.service';
import { TimePeriod } from '../../shared/data/time-period';
import { SpendingService } from './spending-service';

describe('SpendingService', () => {
  let service: SpendingService;

  let spendingValue = signal<SpendingByPeriod | undefined>(undefined);
  let spendingHasValue = signal(false);
  let spendingError = signal<undefined | Error>(undefined);

  let institutionsLoading = signal(false);
  let institutionsError = signal<null | Error>(null);
  let institutionsReloadCount = 0;

  let capturedPeriodSignal: Signal<TimePeriod> | undefined;
  let capturedStartDateSignal: Signal<string> | undefined;
  let reloadCount = 0;

  beforeEach(() => {
    spendingValue = signal(undefined);
    spendingHasValue = signal(false);
    spendingError = signal(undefined);
    institutionsLoading = signal(false);
    institutionsError = signal(null);
    institutionsReloadCount = 0;
    capturedPeriodSignal = undefined;
    capturedStartDateSignal = undefined;
    reloadCount = 0;

    TestBed.configureTestingModule({
      providers: [
        {
          provide: SpendingClient,
          useValue: {
            spendingByPeriodResource: (
              periodSignal: Signal<TimePeriod>,
              startDateSignal: Signal<string>,
            ) => {
              capturedPeriodSignal = periodSignal;
              capturedStartDateSignal = startDateSignal;

              return {
                hasValue: () => spendingHasValue(),
                error: () => spendingError(),
                value: () => spendingValue(),
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
          },
        },
      ],
    });
    service = TestBed.inject(SpendingService);
  });

  it('should default to the monthly period with a start date of today', () => {
    expect(service.selectedPeriod()).toBe('Monthly');
    expect(capturedPeriodSignal?.()).toBe('Monthly');
    expect(capturedStartDateSignal).toBeTruthy();
  });

  it('should report loading until the resource has a value', () => {
    expect(service.loading()).toBe(true);

    spendingHasValue.set(true);

    expect(service.loading()).toBe(false);
  });

  it('should report an error when the resource errors', () => {
    expect(service.error()).toBeFalsy();

    spendingError.set(new Error('boom'));

    expect(service.error()).toBeTruthy();
  });

  it('should keep reporting loading while institutions have not loaded', () => {
    spendingHasValue.set(true);
    institutionsLoading.set(true);

    expect(service.loading()).toBe(true);

    institutionsLoading.set(false);

    expect(service.loading()).toBe(false);
  });

  it('should stop loading when institutions error and the spending data is ready', () => {
    spendingHasValue.set(true);
    institutionsError.set(new Error('no institutions'));

    expect(service.loading()).toBe(false);
  });

  it('should surface institutions fetch errors', () => {
    expect(service.error()).toBeFalsy();

    institutionsError.set(new Error('no institutions'));

    expect(service.error()).toBeTruthy();
  });

  it('should expose the spending data from the resource', () => {
    const spending: SpendingByPeriod = {
      periodStartDate: '2026-08-01',
      periodEndDate: '2026-08-31',
      totalSpend: 1500,
      totalIncome: 3000,
      spendingActivityByCategory: { FOOD: { amount: 500, percentOfSpend: 33.33 } },
    };
    spendingValue.set(spending);
    spendingHasValue.set(true);

    expect(service.data()).toEqual(spending);
  });

  it('should fall back to empty spending data when the resource has no value', () => {
    expect(service.data()).toEqual({
      periodStartDate: '',
      periodEndDate: '',
      totalSpend: 0,
      totalIncome: 0,
      spendingActivityByCategory: {},
    });
  });

  it('should pass period changes through to the resource signal', () => {
    service.selectPeriod('Yearly');

    expect(service.selectedPeriod()).toBe('Yearly');
    expect(capturedPeriodSignal?.()).toBe('Yearly');
  });

  it('should reload the resource on refetch', () => {
    service.refetchAll();

    expect(reloadCount).toBe(1);
    expect(institutionsReloadCount).toBe(1);
  });
});
