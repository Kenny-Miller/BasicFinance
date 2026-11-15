import { TestBed } from '@angular/core/testing';

import { vi } from 'vite-plus/test';

import { TodayService } from './today.service';

describe('TodayService', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 11, 31, 23, 59, 0));
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  it('should start on the current local date', () => {
    const service = TestBed.inject(TodayService);

    expect(service.today().toDateString()).toBe(new Date(2026, 11, 31).toDateString());
  });

  it('should roll over at local midnight', () => {
    const service = TestBed.inject(TodayService);

    vi.advanceTimersByTime(60_000);

    expect(service.today().toDateString()).toBe(new Date(2027, 0, 1).toDateString());
  });

  it('should keep rolling over on following days', () => {
    const service = TestBed.inject(TodayService);

    vi.advanceTimersByTime(60_000 + 24 * 60 * 60 * 1000);

    expect(service.today().toDateString()).toBe(new Date(2027, 0, 2).toDateString());
  });

  it('should catch up when the tab becomes visible after missing midnight', () => {
    const service = TestBed.inject(TodayService);

    // Simulate a throttled tab: the clock moves on without the timer firing.
    vi.setSystemTime(new Date(2027, 0, 1, 9, 0, 0));
    document.dispatchEvent(new Event('visibilitychange'));

    expect(service.today().toDateString()).toBe(new Date(2027, 0, 1).toDateString());
  });
});
