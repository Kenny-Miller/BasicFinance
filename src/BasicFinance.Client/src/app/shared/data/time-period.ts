export type TimePeriod = 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly';

export const TIME_PERIODS: readonly TimePeriod[] = [
  'Weekly',
  'Monthly',
  'Quarterly',
  'Yearly',
];

export const DEFAULT_TIME_PERIOD: TimePeriod = 'Monthly';

export function getTimePeriodLabel(period: TimePeriod): string {
  return period;
}

export function isValidTimePeriod(value: unknown): value is TimePeriod {
  return TIME_PERIODS.includes(value as TimePeriod);
}
