import { httpResource } from '@angular/common/http';
import { Injectable, Signal } from '@angular/core';
import { TimePeriod } from '../../shared/data/time-period';
import { SpendingByPeriod } from './../../shared/api/spending/spending-by-period';

export interface DailySpendingOverTime {
  x: number;
  y: number;
}

export interface SpendingOverTimeSummary {
  currentMonthActivity: DailySpendingOverTime[];
  previousMonthActivity: DailySpendingOverTime[];
  totalMonthlySpend: number;
  monthlySpendDifference: number;
}

@Injectable({
  providedIn: 'root',
})
export class SpendingClient {
  spendingOverTimeSummaryResource() {
    return httpResource<SpendingOverTimeSummary>(() => 'api/Spending/SpendingOverTimeSummary');
  }

  spendingByPeriodResource(periodSignal: Signal<TimePeriod>, startDateSignal: Signal<string>) {
    return httpResource<SpendingByPeriod>(() => {
      const params = {
        startDate: startDateSignal(),
        spendingPeriod: periodSignal(),
      };

      const queryParams = Object.fromEntries(
        Object.entries(params).filter(([_, value]) => value !== undefined),
      );

      return {
        url: 'api/Spending/SpendingActivityByPeriod',
        params: queryParams,
      };
    });
  }
}
