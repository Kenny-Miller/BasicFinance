import { Component, input, output } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { SpendingPeriod } from '../../data/spending-client';

@Component({
  selector: 'app-period-selector',
  imports: [HlmButtonImports, HlmCardImports],
  templateUrl: './period-selector.html',
  styleUrl: './period-selector.css',
})
export class PeriodSelector {
  readonly activePeriod = input.required<SpendingPeriod>();
  readonly periodChange = output<SpendingPeriod>();

  readonly periods: SpendingPeriod[] = ['Weekly', 'Monthly', 'Quarterly', 'Yearly'];

  selectPeriod(period: SpendingPeriod) {
    this.periodChange.emit(period);
  }
}
