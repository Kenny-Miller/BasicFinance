import { computed, inject, Injectable, signal } from '@angular/core';
import { SpendingByPeriod, SpendingClient } from '../../core/data-access/spending-client';
import { PageService } from '../../core/page/page.service';
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
  private readonly pageService = inject(PageService);

  readonly selectedPeriod = signal<TimePeriod>(DEFAULT_TIME_PERIOD);
  private readonly startDate = signal(new Date().toISOString().split('T')[0]);

  private readonly spendingResource = this.spendingClient.spendingByPeriodResource(
    this.selectedPeriod,
    this.startDate,
  );

  readonly loading = computed(
    () => this.pageService.loading() || !this.spendingResource.hasValue(),
  );
  readonly error = computed(() => this.pageService.error() || this.spendingResource.error());
  readonly data = computed<SpendingByPeriod>(() => this.spendingResource.value() ?? EMPTY_SPENDING);

  selectPeriod(period: TimePeriod): void {
    this.selectedPeriod.set(period);
  }

  refetchAll(): void {
    this.pageService.refetchAll();
    this.spendingResource.reload();
  }
}
