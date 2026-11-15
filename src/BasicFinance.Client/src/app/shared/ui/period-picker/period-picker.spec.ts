import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TimePeriod } from '../../data/time-period';
import { PeriodPicker } from './period-picker';

const TODAY = new Date(2026, 10, 14);

@Component({
  selector: 'app-test-host',
  template: `<app-period-picker
    [timePeriod]="timePeriod()"
    [selectedDate]="selectedDate()"
    [today]="today"
    (picked)="picked.push($event)"
  />`,
  imports: [PeriodPicker],
})
class TestHost {
  readonly today = TODAY;
  readonly timePeriod = signal<TimePeriod>('Quarterly');
  readonly selectedDate = signal(TODAY);
  readonly picked: Date[] = [];
}

describe('PeriodPicker', () => {
  let fixture: ComponentFixture<TestHost>;
  let host: TestHost;

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const button = (text: string): HTMLButtonElement =>
    Array.from(element().querySelectorAll<HTMLButtonElement>('button')).find(
      candidate => candidate.textContent?.trim() === text,
    ) as HTMLButtonElement;
  const byLabel = (label: string): HTMLButtonElement =>
    element().querySelector(`button[aria-label="${label}"]`) as HTMLButtonElement;

  const render = (timePeriod: TimePeriod, selectedDate = TODAY): void => {
    host.timePeriod.set(timePeriod);
    host.selectedDate.set(selectedDate);
    fixture.detectChanges();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TestHost] });
    fixture = TestBed.createComponent(TestHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the control that matches the time period', () => {
    render('Weekly');
    expect(element().querySelector('hlm-calendar')).toBeTruthy();
    expect(element().querySelector('hlm-month-year-calendar')).toBeNull();

    render('Monthly');
    expect(element().querySelector('hlm-month-year-calendar')).toBeTruthy();
    expect(element().querySelector('hlm-calendar')).toBeNull();

    render('Quarterly');
    expect(button('Q1')).toBeTruthy();
    expect(button('2026')).toBeUndefined();

    render('Yearly');
    expect(button('2026')).toBeTruthy();
    expect(button('Q1')).toBeUndefined();
  });

  describe('quarters', () => {
    it('should mark the selected quarter and disable future ones', () => {
      expect(button('Q4').getAttribute('aria-pressed')).toBe('true');
      expect(button('Q3').getAttribute('aria-pressed')).toBe('false');

      host.selectedDate.set(new Date(2026, 1, 1));
      fixture.detectChanges();
      byLabel('Previous year').click();
      fixture.detectChanges();
      expect(button('Q4').disabled).toBe(false);

      byLabel('Next year').click();
      fixture.detectChanges();
      expect(byLabel('Next year').disabled).toBe(true);
    });

    it('should pick the start of a quarter in the shown year', () => {
      byLabel('Previous year').click();
      fixture.detectChanges();
      button('Q2').click();

      expect(host.picked).toEqual([new Date(2025, 3, 1)]);
    });
  });

  describe('years', () => {
    it('should show twelve years ending at the current year', () => {
      render('Yearly');

      expect(button('2015')).toBeTruthy();
      expect(button('2026').getAttribute('aria-pressed')).toBe('true');
      expect(byLabel('Next years').disabled).toBe(true);
    });

    it('should page back and pick the start of a year', () => {
      render('Yearly');
      byLabel('Previous years').click();
      fixture.detectChanges();

      button('2010').click();

      expect(host.picked).toEqual([new Date(2010, 0, 1)]);
    });

    it('should pick the start of the current year', () => {
      render('Yearly', new Date(2024, 5, 1));
      button('2026').click();

      expect(host.picked).toEqual([new Date(2026, 0, 1)]);
    });
  });
});
