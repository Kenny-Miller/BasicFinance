import { isCurrentPeriod, periodLabel, periodRange, stepSelectedDate } from './period';

const date = (year: number, month: number, day: number): Date => new Date(year, month - 1, day);

describe('period', () => {
  describe('periodRange', () => {
    it('should start weeks on Monday by default', () => {
      const range = periodRange('Weekly', date(2026, 11, 11));
      expect(range.start).toEqual(date(2026, 11, 9));
      expect(range.end.getDate()).toBe(15);
    });

    it('should follow the weekStartsOn argument', () => {
      const range = periodRange('Weekly', date(2026, 11, 11), 0);
      expect(range.start).toEqual(date(2026, 11, 8));
      expect(range.end.getDate()).toBe(14);
    });

    it('should cover the whole month, quarter and year', () => {
      const selected = date(2026, 11, 14);
      expect(periodRange('Monthly', selected).start).toEqual(date(2026, 11, 1));
      expect(periodRange('Monthly', selected).end.getDate()).toBe(30);
      expect(periodRange('Quarterly', selected).start).toEqual(date(2026, 10, 1));
      expect(periodRange('Yearly', selected).start).toEqual(date(2026, 1, 1));
      expect(periodRange('Yearly', selected).end.getMonth()).toBe(11);
    });
  });

  describe('stepSelectedDate', () => {
    const today = date(2026, 11, 14);

    it('should step back from January 2026 monthly to December 2025', () => {
      const result = stepSelectedDate(date(2026, 1, 15), 'Monthly', 'previous', today);
      expect(result).toEqual(date(2025, 12, 15));
    });

    it('should clamp month-end days to the shorter month', () => {
      const result = stepSelectedDate(date(2026, 3, 31), 'Monthly', 'previous', today);
      expect(result).toEqual(date(2026, 2, 28));
    });

    it('should step by week, quarter and year', () => {
      expect(stepSelectedDate(today, 'Weekly', 'previous', today)).toEqual(date(2026, 11, 7));
      expect(stepSelectedDate(today, 'Quarterly', 'previous', today)).toEqual(date(2026, 8, 14));
      expect(stepSelectedDate(today, 'Yearly', 'previous', today)).toEqual(date(2025, 11, 14));
    });

    it('should never return a date after today', () => {
      const result = stepSelectedDate(date(2026, 10, 31), 'Monthly', 'next', today);
      expect(result).toEqual(today);
    });
  });

  describe('isCurrentPeriod', () => {
    const today = date(2026, 11, 14);

    it('should be true for today and dates in the same period', () => {
      expect(isCurrentPeriod(today, 'Monthly', today)).toBe(true);
      expect(isCurrentPeriod(date(2026, 10, 2), 'Quarterly', today)).toBe(true);
      expect(isCurrentPeriod(date(2026, 1, 1), 'Yearly', today)).toBe(true);
    });

    it('should be false for earlier periods', () => {
      expect(isCurrentPeriod(date(2026, 10, 31), 'Monthly', today)).toBe(false);
      expect(isCurrentPeriod(date(2026, 11, 8), 'Weekly', today)).toBe(false);
    });
  });

  describe('periodLabel', () => {
    it('should label each period', () => {
      expect(periodLabel('Weekly', date(2026, 10, 14))).toBe('Oct 12 – 18');
      expect(periodLabel('Monthly', date(2026, 10, 14))).toBe('October 2026');
      expect(periodLabel('Quarterly', date(2026, 10, 14))).toBe('Q4 2026');
      expect(periodLabel('Yearly', date(2026, 10, 14))).toBe('2026');
    });

    it('should name both months when a week spans two', () => {
      expect(periodLabel('Weekly', date(2026, 10, 1))).toBe('Sep 28 – Oct 4');
    });

    it('should name both years when a week spans two', () => {
      expect(periodLabel('Weekly', date(2025, 12, 31))).toBe('Dec 29, 2025 – Jan 4, 2026');
    });
  });
});
