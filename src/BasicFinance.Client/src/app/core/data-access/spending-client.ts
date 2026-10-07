import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, inject, resource } from '@angular/core';

import { TimePeriod } from '../../shared/data/time-period';
import {
  getApiSpendingActivityByPeriod,
  getApiSpendingOverTimeSummary,
} from '../api/generated/sdk.gen';
import { SPENDING_PERIOD_PARAM } from '../api/period-params';

export type {
  DailySpendingOverTime,
  SpendingActivity,
  SpendingByPeriod,
  SpendingOverTimeSummaryResponse as SpendingOverTimeSummary,
} from '../api/generated/types.gen';

@Injectable({
  providedIn: 'root',
})
export class SpendingClient {
  private readonly httpClient = inject(HttpClient);

  spendingOverTimeSummaryResource() {
    return resource({
      loader: async () => {
        const response = await getApiSpendingOverTimeSummary({
          httpClient: this.httpClient,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }

  spendingByPeriodResource(periodSignal: Signal<TimePeriod>, startDateSignal: Signal<string>) {
    return resource({
      params: () => ({
        StartDate: startDateSignal(),
        SpendingPeriod: SPENDING_PERIOD_PARAM[periodSignal()],
      }),
      loader: async ({ params }) => {
        const response = await getApiSpendingActivityByPeriod({
          httpClient: this.httpClient,
          query: params,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
