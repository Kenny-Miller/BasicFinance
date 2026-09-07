import { Component, OnChanges, input, output } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import {
  DEFAULT_TIME_PERIOD,
  TimePeriod,
  TIME_PERIODS,
  isValidTimePeriod,
} from '../../data/time-period';

export type PeriodNavigation = 'previous' | 'next';

@Component({
  selector: 'app-period-selector',
  imports: [HlmCardImports, HlmToggleGroupImports, HlmFieldImports, HlmButtonImports, NgIcon],
  providers: [provideIcons({ lucideChevronLeft, lucideChevronRight })],
  templateUrl: './period-selector.html',
  styleUrl: './period-selector.css',
})
export class PeriodSelector implements OnChanges {
  readonly activePeriod = input.required<TimePeriod>();
  readonly periodChange = output<TimePeriod>();
  readonly navigate = output<PeriodNavigation>();
  readonly periodLabel = input<string>('');
  readonly nextDisabled = input(false);

  readonly periods = TIME_PERIODS;

  public selectedPeriod: TimePeriod = DEFAULT_TIME_PERIOD;

  public ngOnChanges(): void {
    this.selectedPeriod = this.activePeriod();
  }

  selectPeriod(value: unknown) {
    if (isValidTimePeriod(value)) {
      this.selectedPeriod = value;
      this.periodChange.emit(value);
    }
  }

  navigateTo(direction: PeriodNavigation) {
    this.navigate.emit(direction);
  }
}
