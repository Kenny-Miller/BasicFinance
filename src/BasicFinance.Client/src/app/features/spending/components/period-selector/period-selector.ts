import { Component, OnChanges, input, output } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import { SpendingPeriod } from '../../data/spending-client';

const VALID_PERIODS: readonly SpendingPeriod[] = ['Weekly', 'Monthly', 'Quarterly', 'Yearly'];

@Component({
  selector: 'app-period-selector',
  imports: [HlmCardImports, HlmToggleGroupImports, HlmFieldImports, HlmButtonImports],
  templateUrl: './period-selector.html',
  styleUrl: './period-selector.css',
})
export class PeriodSelector implements OnChanges {
  readonly activePeriod = input.required<SpendingPeriod>();
  readonly periodChange = output<SpendingPeriod>();

  readonly periods = VALID_PERIODS;

  public selectedPeriod: SpendingPeriod = 'Monthly';

  public ngOnChanges(): void {
    this.selectedPeriod = this.activePeriod();
  }

  private isValidPeriod(value: unknown): value is SpendingPeriod {
    return VALID_PERIODS.includes(value as SpendingPeriod);
  }

  selectPeriod(value: unknown) {
    if (this.isValidPeriod(value)) {
      this.periodChange.emit(value);
    }
  }
}
