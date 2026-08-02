import { Component, OnChanges, input, output } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import {
  DEFAULT_TIME_PERIOD,
  TimePeriod,
  TIME_PERIODS,
  isValidTimePeriod,
} from '../../../../shared/data/time-period';

@Component({
  selector: 'app-period-selector',
  imports: [HlmCardImports, HlmToggleGroupImports, HlmFieldImports, HlmButtonImports],
  templateUrl: './period-selector.html',
  styleUrl: './period-selector.css',
})
export class PeriodSelector implements OnChanges {
  readonly activePeriod = input.required<TimePeriod>();
  readonly periodChange = output<TimePeriod>();

  readonly periods = TIME_PERIODS;

  public selectedPeriod: TimePeriod = DEFAULT_TIME_PERIOD;

  public ngOnChanges(): void {
    this.selectedPeriod = this.activePeriod();
  }

  private isValidPeriod(value: unknown): value is TimePeriod {
    return isValidTimePeriod(value);
  }

  selectPeriod(value: unknown) {
    if (this.isValidPeriod(value)) {
      this.periodChange.emit(value);
    }
  }
}
