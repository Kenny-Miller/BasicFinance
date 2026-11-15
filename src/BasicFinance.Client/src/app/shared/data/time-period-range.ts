import {
  type Day,
  addMonths,
  addQuarters,
  addWeeks,
  addYears,
  endOfMonth,
  endOfQuarter,
  endOfWeek,
  endOfYear,
  format,
  getQuarter,
  isSameMonth,
  isSameYear,
  min,
  startOfMonth,
  startOfQuarter,
  startOfWeek,
  startOfYear,
} from 'date-fns';

import { TimePeriod } from './time-period';

export type PeriodDirection = 'previous' | 'next';

export interface PeriodRange {
  start: Date;
  end: Date;
}

/** Monday, matching the backend's `StartOfWeek`. */
export const DEFAULT_WEEK_STARTS_ON: Day = 1;

/** The range of the `timePeriod` that contains `selectedDate`. */
export function periodRange(
  timePeriod: TimePeriod,
  selectedDate: Date,
  weekStartsOn: Day = DEFAULT_WEEK_STARTS_ON,
): PeriodRange {
  switch (timePeriod) {
    case 'Weekly':
      return {
        start: startOfWeek(selectedDate, { weekStartsOn }),
        end: endOfWeek(selectedDate, { weekStartsOn }),
      };
    case 'Monthly':
      return { start: startOfMonth(selectedDate), end: endOfMonth(selectedDate) };
    case 'Quarterly':
      return { start: startOfQuarter(selectedDate), end: endOfQuarter(selectedDate) };
    case 'Yearly':
      return { start: startOfYear(selectedDate), end: endOfYear(selectedDate) };
  }
}

/** Moves `selectedDate` one period back or forward, never past `today`. */
export function stepSelectedDate(
  selectedDate: Date,
  timePeriod: TimePeriod,
  direction: PeriodDirection,
  today: Date = new Date(),
): Date {
  const amount = direction === 'next' ? 1 : -1;
  const stepped = addPeriods(selectedDate, timePeriod, amount);
  return min([stepped, today]);
}

/** True when `selectedDate` falls in the same period as `today`. */
export function isCurrentPeriod(
  selectedDate: Date,
  timePeriod: TimePeriod,
  today: Date = new Date(),
  weekStartsOn: Day = DEFAULT_WEEK_STARTS_ON,
): boolean {
  return (
    periodRange(timePeriod, selectedDate, weekStartsOn).start.getTime() ===
    periodRange(timePeriod, today, weekStartsOn).start.getTime()
  );
}

/** "Oct 12 – 18", "October 2026", "Q4 2026", "2026". */
export function periodLabel(
  timePeriod: TimePeriod,
  selectedDate: Date,
  weekStartsOn: Day = DEFAULT_WEEK_STARTS_ON,
): string {
  switch (timePeriod) {
    case 'Weekly':
      return weekLabel(periodRange(timePeriod, selectedDate, weekStartsOn));
    case 'Monthly':
      return format(selectedDate, 'MMMM yyyy');
    case 'Quarterly':
      return `Q${getQuarter(selectedDate)} ${format(selectedDate, 'yyyy')}`;
    case 'Yearly':
      return format(selectedDate, 'yyyy');
  }
}

function addPeriods(date: Date, timePeriod: TimePeriod, amount: number): Date {
  switch (timePeriod) {
    case 'Weekly':
      return addWeeks(date, amount);
    case 'Monthly':
      return addMonths(date, amount);
    case 'Quarterly':
      return addQuarters(date, amount);
    case 'Yearly':
      return addYears(date, amount);
  }
}

function weekLabel({ start, end }: PeriodRange): string {
  if (!isSameYear(start, end)) {
    return `${format(start, 'MMM d, yyyy')} – ${format(end, 'MMM d, yyyy')}`;
  }
  if (!isSameMonth(start, end)) {
    return `${format(start, 'MMM d')} – ${format(end, 'MMM d')}`;
  }
  return `${format(start, 'MMM d')} – ${format(end, 'd')}`;
}
