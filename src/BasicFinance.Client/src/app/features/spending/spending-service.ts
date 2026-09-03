import { computed, inject, Injectable, signal } from '@angular/core';
import { SpendingClient } from '../../core/data-access/spending-client';
import { SpendingByPeriod } from '../../shared/api/spending/spending-by-period';
import { DEFAULT_TIME_PERIOD, TimePeriod } from '../../shared/data/time-period';

const EMPTY_SPENDING: SpendingByPeriod = {
  periodStartDate: '',
  periodEndDate: '',
  totalSpend: 0,
  totalIncome: 0,
  spendingActivityByCategory: {},
};

@Injectable({
  providedIn: 'root',
})
export class SpendingService {
  private readonly spendingClient = inject(SpendingClient);

  readonly selectedPeriod = signal<TimePeriod>(DEFAULT_TIME_PERIOD);
  private readonly startDate = signal(new Date().toISOString().split('T')[0]);

  private readonly spendingResource = this.spendingClient.spendingByPeriodResource(
    this.selectedPeriod,
    this.startDate,
  );

  readonly loading = computed(() => !this.spendingResource.hasValue());
  readonly error = computed(() => this.spendingResource.error());
  readonly data = computed<SpendingByPeriod>(() => this.spendingResource.value() ?? EMPTY_SPENDING);

  selectPeriod(period: TimePeriod): void {
    this.selectedPeriod.set(period);
  }

  refetchAll(): void {
    this.spendingResource.reload();
  }
}
