import { format } from 'date-fns';

/** Formats a local date as the API's `DateOnly` string (`yyyy-MM-dd`). */
export function formatDateOnly(date: Date): string;
export function formatDateOnly(date: Date | null): string | undefined;
export function formatDateOnly(date: Date | null): string | undefined {
  return date ? format(date, 'yyyy-MM-dd') : undefined;
}
