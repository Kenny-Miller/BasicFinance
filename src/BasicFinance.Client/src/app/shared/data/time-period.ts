import type { TimePeriod } from '../../core/api/time-period';

export type { TimePeriod };

export interface TimePeriodConfig {
  /** Segment label in the period picker ("Month"). */
  label: string;
  /** Comparison label for deltas ("last month"). */
  previousLabel: string;
}

/**
 * Display text per period, in display order. Typed as a Record so a period added to the API
 * fails to compile here until it has labels.
 */
export const TIME_PERIOD_CONFIG = {
  Weekly: { label: 'Week', previousLabel: 'last week' },
  Monthly: { label: 'Month', previousLabel: 'last month' },
  Quarterly: { label: 'Quarter', previousLabel: 'last quarter' },
  Yearly: { label: 'Year', previousLabel: 'last year' },
} satisfies Record<TimePeriod, TimePeriodConfig>;

export const TIME_PERIODS: readonly TimePeriod[] = Object.keys(TIME_PERIOD_CONFIG) as TimePeriod[];

export const DEFAULT_TIME_PERIOD: TimePeriod = 'Yearly';

export function isValidTimePeriod(value: unknown): value is TimePeriod {
  return TIME_PERIODS.includes(value as TimePeriod);
}
