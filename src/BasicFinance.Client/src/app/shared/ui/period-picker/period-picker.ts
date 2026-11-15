import { Component, computed, input, linkedSignal, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCalendarImports } from '@spartan-ng/helm/calendar';
import { type Day, getQuarter, getYear, min } from 'date-fns';

import { DEFAULT_WEEK_STARTS_ON } from '../../data/period';
import { TimePeriod } from '../../data/time-period';

const YEARS_PER_PAGE = 12;

/**
 * Popover body that lets the user jump straight to a period:
 * a day calendar for weeks, a month grid, a quarter grid, or a year grid.
 */
@Component({
  selector: 'app-period-picker',
  imports: [NgIcon, HlmButtonImports, HlmCalendarImports],
  providers: [provideIcons({ lucideChevronLeft, lucideChevronRight })],
  templateUrl: './period-picker.html',
  styleUrl: './period-picker.css',
})
export class PeriodPicker {
  readonly timePeriod = input.required<TimePeriod>();
  readonly selectedDate = input.required<Date>();
  readonly today = input.required<Date>();
  readonly weekStartsOn = input<Day>(DEFAULT_WEEK_STARTS_ON);

  /** Emits a date inside the chosen period, never after `today`. */
  readonly picked = output<Date>();

  readonly quarters = [1, 2, 3, 4] as const;

  /** Year shown in the quarter grid header. */
  readonly quarterYear = linkedSignal(() => getYear(this.selectedDate()));

  /** Last year shown in the year grid; paged by `YEARS_PER_PAGE`. */
  readonly yearPageEnd = linkedSignal(() => getYear(this.today()));

  readonly todayYear = computed(() => getYear(this.today()));

  readonly years = computed(() =>
    Array.from(
      { length: YEARS_PER_PAGE },
      (_, index) => this.yearPageEnd() - YEARS_PER_PAGE + 1 + index,
    ),
  );

  pickDate(date: Date | undefined): void {
    if (date) {
      this.picked.emit(min([date, this.today()]));
    }
  }

  pickQuarter(quarter: number): void {
    this.pickDate(new Date(this.quarterYear(), (quarter - 1) * 3, 1));
  }

  pickYear(year: number): void {
    this.pickDate(new Date(year, 0, 1));
  }

  isQuarterDisabled(quarter: number): boolean {
    return new Date(this.quarterYear(), (quarter - 1) * 3, 1) > this.today();
  }

  isQuarterSelected(quarter: number): boolean {
    const selected = this.selectedDate();
    return getYear(selected) === this.quarterYear() && getQuarter(selected) === quarter;
  }

  isYearSelected(year: number): boolean {
    return getYear(this.selectedDate()) === year;
  }

  shiftQuarterYear(amount: number): void {
    this.quarterYear.update(year => Math.min(year + amount, this.todayYear()));
  }

  shiftYearPage(amount: number): void {
    this.yearPageEnd.update(year => Math.min(year + amount * YEARS_PER_PAGE, this.todayYear()));
  }
}
