import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

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
  createSpendingOverTimeSummaryResource() {
    return httpResource<SpendingOverTimeSummary>(
      () => 'api/Spending/SpendingOverTimeSummary',
    );
  }
}
