import { formatDateOnly } from './date-only';

describe('formatDateOnly', () => {
  it('should format the local date as yyyy-MM-dd', () => {
    expect(formatDateOnly(new Date(2025, 1, 10, 23, 30))).toBe('2025-02-10');
  });

  it('should return undefined for null', () => {
    expect(formatDateOnly(null)).toBeUndefined();
  });
});
