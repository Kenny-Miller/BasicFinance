import { httpResource } from '@angular/common/http';
import { Injectable, Signal } from '@angular/core';
import { SpendingByPeriod } from '../../../shared/api/spending/spending-by-period';

export type SpendingPeriod = 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly';

@Injectable({
  providedIn: 'root',
})
export class SpendingClient {
  createResource(periodSignal: Signal<SpendingPeriod>, startDateSignal: Signal<string>) {
    return httpResource<SpendingByPeriod>(() => {
      const params = new URLSearchParams();
      params.set('startDate', startDateSignal());
      params.set('spendingPeriod', periodSignal());
      return `api/Spending/SpendingActivityByPeriod?${params.toString()}`;
    });
  }
}
