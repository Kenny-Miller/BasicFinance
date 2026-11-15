import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TimePeriod } from '../../data/time-period';
import { PeriodStepper } from './period-stepper';

const TODAY = new Date(2026, 10, 14);

@Component({
  selector: 'app-test-host',
  template: `<app-period-stepper
    [(selectedTimePeriod)]="selectedTimePeriod"
    [(selectedDate)]="selectedDate"
    [today]="today"
  />`,
  imports: [PeriodStepper],
})
class TestHost {
  readonly today = TODAY;
  readonly selectedTimePeriod = signal<TimePeriod>('Yearly');
  readonly selectedDate = signal(TODAY);
}

describe('PeriodStepper', () => {
  let fixture: ComponentFixture<TestHost>;
  let host: TestHost;

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const previous = (): HTMLButtonElement =>
    element().querySelector('button[aria-label="Previous period"]') as HTMLButtonElement;
  const next = (): HTMLButtonElement =>
    element().querySelector('button[aria-label="Next period"]') as HTMLButtonElement;
  const segment = (label: string): HTMLButtonElement =>
    Array.from(
      element().querySelectorAll<HTMLButtonElement>('button[data-slot="toggle-group-item"]'),
    ).find(button => button.textContent?.trim() === label) as HTMLButtonElement;
  const label = (): string | undefined => element().querySelector('span')?.textContent?.trim();

  const click = (button: HTMLButtonElement): void => {
    button.click();
    fixture.detectChanges();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TestHost] });
    fixture = TestBed.createComponent(TestHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should label the selected period', () => {
    expect(label()).toBe('2026');
  });

  it('should disable next at the current period', () => {
    expect(next().disabled).toBe(true);
  });

  it('should step the bound selectedDate back and forward', () => {
    click(previous());
    expect(host.selectedDate()).toEqual(new Date(2025, 10, 14));
    expect(label()).toBe('2025');
    expect(next().disabled).toBe(false);

    click(next());
    expect(host.selectedDate()).toEqual(TODAY);
  });

  it('should reset selectedDate to today when the time period changes', () => {
    host.selectedTimePeriod.set('Quarterly');
    host.selectedDate.set(new Date(2025, 1, 10));
    fixture.detectChanges();
    expect(label()).toBe('Q1 2025');

    click(segment('Month'));

    expect(host.selectedTimePeriod()).toBe('Monthly');
    expect(host.selectedDate()).toEqual(TODAY);
    expect(label()).toBe('November 2026');
  });

  it('should mark the selected time period as pressed', () => {
    expect(segment('Year').getAttribute('aria-pressed')).toBe('true');
    expect(segment('Month').getAttribute('aria-pressed')).toBe('false');
  });

  it('should keep the time period and date when the selected option is clicked again', () => {
    click(previous());
    const steppedDate = host.selectedDate();

    click(segment('Year'));

    expect(host.selectedTimePeriod()).toBe('Yearly');
    expect(host.selectedDate()).toEqual(steppedDate);
    expect(segment('Year').getAttribute('aria-pressed')).toBe('true');
  });
});
