import { httpResource } from '@angular/common/http';
import { Injectable, Signal } from '@angular/core';
import { TimePeriod } from '../../shared/data/time-period';

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

export interface SpendingActivity {
  amount: number;
  percentOfSpend: number;
}

export interface SpendingByPeriod {
  periodStartDate: string;
  periodEndDate: string;
  totalSpend: number;
  totalIncome: number;
  spendingActivityByCategory: Record<string, SpendingActivity>;
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
