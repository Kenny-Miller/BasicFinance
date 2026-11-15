import { Component, computed, inject, input, model, signal } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronDown, lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import type { Day } from 'date-fns';

import { TodayService } from '../../../core/time/today.service';
import {
  DEFAULT_WEEK_STARTS_ON,
  PeriodDirection,
  isCurrentPeriod,
  periodLabel,
  stepSelectedDate,
} from '../../data/period';
import {
  TIME_PERIOD_CONFIG,
  TIME_PERIODS,
  TimePeriod,
  isValidTimePeriod,
} from '../../data/time-period';
import { PeriodPicker } from '../period-picker/period-picker';

const TIME_PERIOD_OPTIONS: readonly { value: TimePeriod; label: string }[] = TIME_PERIODS.map(
  value => ({
    value,
    label: TIME_PERIOD_CONFIG[value].label,
  }),
);

@Component({
  selector: 'app-period-stepper',
  imports: [NgIcon, HlmToggleGroupImports, HlmButtonImports, HlmPopoverImports, PeriodPicker],
  providers: [provideIcons({ lucideChevronDown, lucideChevronLeft, lucideChevronRight })],
  templateUrl: './period-stepper.html',
  styleUrl: './period-stepper.css',
})
export class PeriodStepper {
  private readonly todayService = inject(TodayService);

  readonly selectedTimePeriod = model.required<TimePeriod>();
  readonly selectedDate = model.required<Date>();
  readonly weekStartsOn = input<Day>(DEFAULT_WEEK_STARTS_ON);
  /** Pins "today" (tests, previews); otherwise follows `TodayService`, which rolls over at midnight. */
  readonly todayOverride = input<Date | undefined>(undefined, { alias: 'today' });
  readonly today = computed(() => this.todayOverride() ?? this.todayService.today());

  readonly timePeriodOptions = TIME_PERIOD_OPTIONS;

  readonly pickerState = signal<'open' | 'closed'>('closed');

  readonly label = computed(() =>
    periodLabel(this.selectedTimePeriod(), this.selectedDate(), this.weekStartsOn()),
  );

  readonly nextDisabled = computed(() =>
    isCurrentPeriod(
      this.selectedDate(),
      this.selectedTimePeriod(),
      this.today(),
      this.weekStartsOn(),
    ),
  );

  /**
   * Changes the selected time period and resets the selected date to today.
   * @param timePeriod
   * @returns
   */
  changeTimePeriod(timePeriod: unknown): void {
    if (!isValidTimePeriod(timePeriod) || timePeriod === this.selectedTimePeriod()) {
      return;
    }
    this.selectedDate.set(this.today());
    this.selectedTimePeriod.set(timePeriod);
  }

  /**
   * Steps the selected date forward or backward by one period.
   * @param direction
   * @returns
   */
  step(direction: PeriodDirection): void {
    if (direction === 'next' && this.nextDisabled()) {
      return;
    }
    this.selectedDate.set(
      stepSelectedDate(this.selectedDate(), this.selectedTimePeriod(), direction, this.today()),
    );
  }

  /**
   * Picks a date from the period picker and closes the picker.
   * @param date
   */
  pickDate(date: Date): void {
    this.selectedDate.set(date);
    this.pickerState.set('closed');
  }
}
